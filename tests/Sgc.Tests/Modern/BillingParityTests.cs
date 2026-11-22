using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;
using Sgc.Novo.Billing;
using Sgc.Tests.Characterization;
using Sgc.Tests.Infra;
using Sgc.Tests.Spikes;
using Xunit.Abstractions;
using NewCalendar = Sgc.Novo.Billing.BusinessCalendar;

namespace Sgc.Tests.Modern;

/// <summary>
/// The billing rule of the new system (modern/src/Sgc.Novo/Billing, compiled as-is) against the legacy
/// implementations executed as the oracle. Every difference must belong to a category the business decided
/// (ADR-005); anything else fails.
/// </summary>
[Trait("Area", "Modern")]
public class BillingParityTests(ITestOutputHelper output)
{
    static NewCalendar Calendar()
    {
        using var cn = new SqlConnection(TestEnvironment.ConnectionString);
        return new NewCalendar(cn.Query<DateTime>("SELECT DT_FERIADO FROM TB_FERIADO"));
    }

    [Fact(DisplayName = "M-1 · boleto: the new implementation equals the legacy VB.NET one on every issued installment and on the factor edges")]
    public void BoletoParity()
    {
        using var cn = new SqlConnection(TestEnvironment.ConnectionString);
        var installments = cn.Query<(DateTime Due, decimal Value, string NossoNumero)>(
            "SELECT DT_VENCIMENTO, VL_PARCELA, NR_NOSSO_NUMERO FROM TB_PARCELA WHERE NR_NOSSO_NUMERO IS NOT NULL").ToList();
        var edges = new[] { new DateTime(2025, 2, 21), new DateTime(2025, 2, 22), new DateTime(2030, 1, 1), new DateTime(2049, 10, 1), new DateTime(2050, 1, 1) };
        var all = installments.Concat(edges.Select(d => (Due: d, Value: 100m, NossoNumero: "00000000019"))).ToList();

        var legacy = VbNetOracle.Run(all.Select(x => VbNetOracle.BoletoCase(x.Due, x.Value, x.NossoNumero)).ToList());
        var differences = all.Where((x, i) => Boleto.TypeableLine(Boleto.Barcode(x.Due, x.Value, "0001", "17", x.NossoNumero, "123456")) != legacy[i]).ToList();

        output.WriteLine($"{installments.Count} installments + {edges.Length} edge dates: {differences.Count} difference(s)");
        Assert.Empty(differences);
    }

    [Fact(DisplayName = "M-2 · late fees: every difference from the legacy implementations is one the business decided")]
    public void LateFeeParity()
    {
        var calendar = Calendar();
        var newRule = new LateFeeCalculator(calendar);
        var screen = new PaymentScreenRule(Characterization.BusinessCalendar.FromDatabase());
        var vb6 = new Vb6Oracle();

        using var cn = new SqlConnection(TestEnvironment.ConnectionString);
        var values = cn.Query<decimal>("SELECT DISTINCT VL_PARCELA FROM TB_PARCELA WHERE VL_PARCELA IS NOT NULL").ToList();
        values.AddRange([1562.25m, 1037.25m, 812.75m, 99.99m]);
        var dueDates = new List<DateTime>();
        for (var d = new DateTime(2024, 1, 1); d <= new DateTime(2027, 12, 31); d = d.AddDays(1))
            if (d.Day is 5 or 10 or 15 or 25) dueDates.Add(d);
        dueDates.AddRange(cn.Query<DateTime>("SELECT DT_FERIADO FROM TB_FERIADO"));
        var cases = (from v in values from due in dueDates from late in Enumerable.Range(0, 46)
                     select (Value: v, Due: due, Paid: due.AddDays(late), Late: late)).ToList();
        var batch = VbNetOracle.Run(cases.Select(x => VbNetOracle.LateFeeCase(x.Value, x.Due, x.Paid)).ToList());

        var decided = new Dictionary<string, int>();
        var undecided = new List<string>();
        void Decided(string category) => decided[category] = decided.GetValueOrDefault(category) + 1;
        static bool HalfCentUp((decimal Fine, decimal Interest) mine, (decimal Fine, decimal Interest) other) =>
            mine.Fine - other.Fine is 0m or 0.01m && mine.Interest - other.Interest is 0m or 0.01m;

        for (var i = 0; i < cases.Count; i++)
        {
            var (v, due, paid, late) = cases[i];
            var n = newRule.Calculate(v, due, paid);
            var mine = (n.Fine, n.Interest);
            var where = $"{v:N2} due {due:dd/MM/yyyy} paid {paid:dd/MM/yyyy}";

            var s = screen.Calculate(v, due, paid);
            if (s != mine)
            {
                if (HalfCentUp(mine, s)) Decided("vs payment screen: half a cent rounds up (decision 4)");
                else undecided.Add($"screen {where}: {s} vs new {mine}");
            }

            var b = VbNetOracle.ParseLateFee(batch[i]);
            if (b != mine)
            {
                if (mine == (0m, 0m) && b.Fine > 0) Decided("vs batch: no fee within the grace day after a holiday (decision 2)");
                else undecided.Add($"batch {where}: {b} vs new {mine}");
            }

            if (late <= 12 || late % 5 == 0)
            {
                var p = vb6.LateFee(v, due, paid);
                if (p != mine)
                {
                    if (mine == (0m, 0m) && p.Fine > 0) Decided("vs portal: no fee within the grace day (decision 2)");
                    else if (HalfCentUp(mine, p)) Decided("vs portal: half a cent rounds up (decision 4)");
                    else undecided.Add($"portal {where}: {p} vs new {mine}");
                }
            }
        }

        output.WriteLine($"{cases.Count:N0} cases");
        foreach (var (category, n) in decided.OrderByDescending(x => x.Value)) output.WriteLine($"  {category}: {n:N0}");
        foreach (var u in undecided.Take(10)) output.WriteLine("  UNDECIDED " + u);
        Assert.Empty(undecided);
    }
}

/// <summary>Posting payments through the legacy procedure with the rounding record, on a database copy.</summary>
[Collection("poc06")]
[Trait("Area", "Modern")]
public class PaymentPostingTests(BancoPoc06 database, ITestOutputHelper output)
{
    public static void ApplyModernSchema()
    {
        var script = File.ReadAllText(TestEnvironment.PathFromRoot("modern", "sql", "001_rounding_record.sql"));
        using var cn = new SqlConnection(BancoPoc06.Conexao());
        foreach (var batch in System.Text.RegularExpressions.Regex.Split(script, @"^\s*GO\s*$", System.Text.RegularExpressions.RegexOptions.Multiline))
            if (!string.IsNullOrWhiteSpace(batch)) cn.Execute(batch);
    }

    [Fact(DisplayName = "M-3 · rounding record: written in the same transaction as the payment, and what it costs")]
    public async Task RoundingRecord()
    {
        database.Restaurar();
        ApplyModernSchema();
        using var cn = new SqlConnection(BancoPoc06.Conexao());
        var rule = new LateFeeCalculator(new NewCalendar(cn.Query<DateTime>("SELECT DT_FERIADO FROM TB_FERIADO")));
        var installments = cn.Query<(int Id, DateTime Due, decimal Value)>(
            "SELECT TOP 400 CD_PARCELA, DT_VENCIMENTO, VL_PARCELA FROM TB_PARCELA WHERE ID_SITUACAO = 2 ORDER BY CD_PARCELA").ToList();
        Assert.True(installments.Count >= 200, $"too few issued installments in the copy: {installments.Count}");
        var posting = new PaymentPosting(BancoPoc06.Conexao());
        await posting.PostAsync(-1, DateTime.Today, 0, 0, 0, 1);   // warms up the pool and the procedure's plan

        var without = new List<double>(); var with = new List<double>();
        for (var i = 0; i < installments.Count; i++)
        {
            var (id, due, value) = installments[i];
            var paidOn = due.AddDays(3 + i % 40);   // always late: always with fees
            var fee = rule.Calculate(value, due, paidOn);
            var sw = Stopwatch.StartNew();
            var r = i % 2 == 0
                ? await posting.PostAsync(id, paidOn, fee.Total(value), fee.Fine, fee.Interest, 1)
                : await posting.PostAsync(id, due, value, paidOn, fee.Total(value), 1, rule);
            (i % 2 == 0 ? without : with).Add(sw.Elapsed.TotalMilliseconds);
            Assert.Equal(PostingStatus.Posted, r.Status);
        }

        static double Median(List<double> x) { x.Sort(); return x[x.Count / 2]; }
        var records = cn.Query<(int Id, decimal Fine, decimal Interest, decimal Difference, decimal PostedFine, decimal PostedInterest)>("""
            SELECT R.CD_PARCELA, R.VL_MULTA, R.VL_JUROS, R.VL_DIFERENCA, P.VL_MULTA, P.VL_JUROS
              FROM novo.TB_ENCARGO_ARREDONDAMENTO R JOIN TB_PARCELA P ON P.CD_PARCELA = R.CD_PARCELA
            """).ToList();
        var size = cn.QuerySingle<(long Rows, long Kb)>("SELECT SUM(CASE WHEN index_id IN (0, 1) THEN row_count END), SUM(used_page_count) * 8 FROM sys.dm_db_partition_stats WHERE object_id = OBJECT_ID('novo.TB_ENCARGO_ARREDONDAMENTO')");
        output.WriteLine($"{installments.Count} payments; median without record {Median(without):N2} ms, with record {Median(with):N2} ms");
        output.WriteLine($"{records.Count} records, {records.Count(x => x.Difference != 0)} changed by the decision (total R$ {records.Sum(x => x.Difference):N2}); {size.Kb} KB for {size.Rows} rows");

        Assert.Equal(with.Count, records.Count);
        Assert.All(records, x => Assert.Equal((x.Fine, x.Interest), (x.PostedFine, x.PostedInterest)));
        Assert.All(records, x => Assert.InRange(x.Difference, 0m, 0.02m));   // half up is never below banker's

        // a refused payment leaves no record (same transaction)
        var paid = installments[1];
        var before = records.Count;
        var again = await posting.PostAsync(paid.Id, paid.Due, paid.Value, paid.Due.AddDays(10), paid.Value * 2, 1, rule);
        Assert.Equal(PostingStatus.Conflict, again.Status);
        Assert.Equal(before, cn.ExecuteScalar<int>("SELECT COUNT(*) FROM novo.TB_ENCARGO_ARREDONDAMENTO"));
    }
}
