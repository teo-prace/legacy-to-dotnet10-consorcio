using System.Globalization;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Playwright;
using Sgc.Tests.Infra;
using Xunit.Abstractions;

namespace Sgc.Tests.Characterization;

/// <summary>
/// Characterization of the billing rules: what each legacy implementation does today, right or wrong.
/// Nothing new is tested here; the legacy implementations are compared with each other, executed:
///   - VB.NET (SGC.Negocio boleto, SGC.Batch late fees) through tools/oracle on .NET Framework 4.8
///   - VB6 (the portal's SGCBoleto.dll) through COM
///   - the payment screen through the browser
/// These numbers back findings R1 to R6 in assessment/findings.md.
/// </summary>
[Collection("browser")]
[Trait("Area", "Characterization")]
public class BillingCharacterizationTests(BrowserFixture browser, ITestOutputHelper output)
{
    static readonly CultureInfo PtBr = new("pt-BR");

    [Fact(DisplayName = "C-1 · boleto: VB.NET and VB6 agree on every issued installment, and part ways in 2050")]
    public void BoletoVbNetVersusVb6()
    {
        using var cn = new SqlConnection(TestEnvironment.ConnectionString);
        var installments = cn.Query<(DateTime Due, decimal Value, string NossoNumero)>(
            "SELECT DT_VENCIMENTO, VL_PARCELA, NR_NOSSO_NUMERO FROM TB_PARCELA WHERE NR_NOSSO_NUMERO IS NOT NULL").ToList();
        // due-date factor edges: the 2025 reset (21 and 22/02/2025) and the next one (2049/2050)
        var edges = new[] { new DateTime(2025, 2, 21), new DateTime(2025, 2, 22), new DateTime(2030, 1, 1), new DateTime(2049, 10, 1), new DateTime(2050, 1, 1) };
        var all = installments.Concat(edges.Select(d => (Due: d, Value: 100m, NossoNumero: "00000000019"))).ToList();

        var vbnet = VbNetOracle.Run(all.Select(x => VbNetOracle.BoletoCase(x.Due, x.Value, x.NossoNumero)).ToList());
        var vb6 = new Vb6Oracle();
        var differences = new List<DateTime>();
        for (var i = 0; i < all.Count; i++)
        {
            var (due, value, nn) = all[i];
            var fromVb6 = vb6.TypeableLine(due, value, nn);
            if (fromVb6 != vbnet[i])
            {
                differences.Add(due);
                output.WriteLine($"due {due:dd/MM/yyyy}: VB.NET {vbnet[i]} / VB6 {fromVb6}");
            }
        }
        output.WriteLine($"{installments.Count} issued installments + {edges.Length} edge dates: {differences.Count} difference(s)");

        // finding R6: same result today, VB6 breaks when the due-date factor resets again
        Assert.Equal([new DateTime(2050, 1, 1)], differences);
    }

    [Fact(DisplayName = "C-2 · late fees: payment screen x bank return batch x portal, over 333,960 cases")]
    public void LateFeeMatrix()
    {
        var screen = new PaymentScreenRule(BusinessCalendar.FromDatabase());
        var vb6 = new Vb6Oracle();

        using var cn = new SqlConnection(TestEnvironment.ConnectionString);
        var values = cn.Query<decimal>("SELECT DISTINCT VL_PARCELA FROM TB_PARCELA WHERE VL_PARCELA IS NOT NULL").ToList();
        values.AddRange([1562.25m, 1037.25m, 812.75m, 99.99m]);   // values that land on half a cent
        var dueDates = new List<DateTime>();
        for (var d = new DateTime(2024, 1, 1); d <= new DateTime(2027, 12, 31); d = d.AddDays(1))
            if (d.Day is 5 or 10 or 15 or 25) dueDates.Add(d);              // the groups' due days
        dueDates.AddRange(cn.Query<DateTime>("SELECT DT_FERIADO FROM TB_FERIADO"));   // and every holiday

        var cases = (from v in values from due in dueDates from late in Enumerable.Range(0, 46)
                     select (Value: v, Due: due, Paid: due.AddDays(late), Late: late)).ToList();
        var batch = VbNetOracle.Run(cases.Select(x => VbNetOracle.LateFeeCase(x.Value, x.Due, x.Paid)).ToList());

        var counts = new Dictionary<string, int>();
        var examples = new Dictionary<string, string>();
        void Count(string category, string example) { counts[category] = counts.GetValueOrDefault(category) + 1; examples.TryAdd(category, example); }

        for (var i = 0; i < cases.Count; i++)
        {
            var (v, due, paid, late) = cases[i];
            var s = screen.Calculate(v, due, paid);
            var b = VbNetOracle.ParseLateFee(batch[i]);
            var head = $"installment {v:N2}, due {due:ddd dd/MM/yyyy}, paid {paid:ddd dd/MM/yyyy}: screen {s.Fine:N2}+{s.Interest:N2}";
            if (b != s)
                Count(s.Fine == 0 && b.Fine > 0 ? "batch charges, screen doesn't (holiday) [R2]" : "batch: different cents (rounding) [R4]",
                      $"{head}; batch {b.Fine:N2}+{b.Interest:N2}");
            if (late <= 12 || late % 5 == 0)   // COM is slow: sample the VB6 component
            {
                var p = vb6.LateFee(v, due, paid);
                if (p != s)
                    Count(s.Fine == 0 && p.Fine > 0 ? "portal charges, screen doesn't (no grace day) [R3]" : "portal: different cents [R4]",
                          $"{head}; portal {p.Fine:N2}+{p.Interest:N2}");
            }
        }

        output.WriteLine($"{cases.Count:N0} cases ({values.Count} values x {dueDates.Count} due dates x 0-45 days late)");
        foreach (var (category, n) in counts.OrderByDescending(x => x.Value))
            output.WriteLine($"  {category}: {n:N0}   e.g. {examples[category]}");

        Assert.True(counts.GetValueOrDefault("batch charges, screen doesn't (holiday) [R2]") > 0);
        Assert.True(counts.GetValueOrDefault("portal charges, screen doesn't (no grace day) [R3]") > 0);
        Assert.True(counts.GetValueOrDefault("batch: different cents (rounding) [R4]") > 0);
    }

    [Fact(DisplayName = "C-3 · the payment screen rule above is what the real screen does (checked in the browser)")]
    public async Task PaymentScreenRuleMatchesTheScreen()
    {
        await TestEnvironment.EnsureLegacyAsync();
        var rule = new PaymentScreenRule(BusinessCalendar.FromDatabase());
        using var cn = new SqlConnection(TestEnvironment.ConnectionString);
        var installments = cn.Query<(int Id, DateTime Due, decimal Value)>(
            "SELECT TOP 6 CD_PARCELA, DT_VENCIMENTO, VL_PARCELA FROM TB_PARCELA WHERE ID_SITUACAO = 2 AND DT_VENCIMENTO < GETDATE() ORDER BY DT_VENCIMENTO, CD_PARCELA").ToList();
        var holiday = new DateTime(2026, 11, 20);   // a Friday holiday: paid on the day and on the next business day

        var page = await browser.NewPageAsync();
        await BrowserFixture.LoginAsync(page, TestEnvironment.LegacyUrl);
        int checkedCases = 0, equal = 0;
        foreach (var (id, due, value) in installments)
            foreach (var paid in new[] { due, due.AddDays(1), due.AddDays(2), due.AddDays(3), due.AddDays(9), due.AddDays(31), holiday, holiday.AddDays(3) })
            {
                if (paid < due || paid > DateTime.Today) continue;
                await page.GotoAsync($"{TestEnvironment.LegacyUrl}/Cobranca/Baixa.aspx?id={id}");
                await page.FillAsync("#ctl00_cphConteudo_txtDataPagamento", paid.ToString("dd/MM/yyyy"));
                await Task.WhenAll(page.WaitForLoadStateAsync(LoadState.NetworkIdle), page.ClickAsync("#ctl00_cphConteudo_btnCalcular"));
                var fine = decimal.Parse((await page.InnerTextAsync("xpath=//td[text()='Multa:']/following-sibling::td")).Replace("R$", "").Trim(), PtBr);
                var interest = decimal.Parse((await page.InnerTextAsync("xpath=//td[text()='Juros:']/following-sibling::td")).Split('(')[0].Replace("R$", "").Trim(), PtBr);
                checkedCases++;
                if ((fine, interest) == rule.Calculate(value, due, paid)) equal++;
                else output.WriteLine($"NOT UNDERSTOOD: {value:N2} due {due:dd/MM/yyyy} paid {paid:dd/MM/yyyy}: screen {fine:N2}+{interest:N2}");
            }
        output.WriteLine($"{checkedCases} cases checked on the screen, {equal} match the rule");
        Assert.True(checkedCases >= 30);
        Assert.Equal(checkedCases, equal);
    }
}
