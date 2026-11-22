using Dapper;
using Microsoft.Data.SqlClient;

namespace Sgc.Novo.Data;

/// <summary>Filters for the installment search; same parameter names as the legacy screen (Parcelas.aspx?grupo=4).</summary>
public record InstallmentFilter(int? Group, string Status, DateTime? From, DateTime? To, string? NossoNumero, int Page = 1)
{
    /// <summary>Status: V overdue (default), 1 open, 2 issued, 3 paid, T all.</summary>
    public static readonly InstallmentFilter Default = new(null, "V", null, null, null);
}

public class InstallmentRow
{
    public int CdParcela { get; init; }
    public int CdCota { get; init; }
    public string DsCota { get; init; } = "";
    public string NmConsorciado { get; init; } = "";
    public int NrParcela { get; init; }
    public DateTime? DtVencimento { get; init; }
    public string? NrNossoNumero { get; init; }
    public decimal? VlParcela { get; init; }
    public DateTime? DtPagamento { get; init; }
    public decimal? VlPago { get; init; }
    public int? QtDiasAtraso { get; init; }
    public int IdSituacao { get; init; }
    public long Ordem { get; init; }
    public int Total { get; init; }
    public string StatusText => InstallmentStatus.Text(IdSituacao);
}

public class InstallmentForPayment
{
    public int CdParcela { get; init; }
    public int CdCota { get; init; }
    public string DsCota { get; init; } = "";
    public string NmConsorciado { get; init; } = "";
    public int NrParcela { get; init; }
    public int QtPrazoCota { get; init; }
    public string? NrNossoNumero { get; init; }
    public DateTime DtVencimento { get; init; }
    public decimal VlParcela { get; init; }
    public int IdSituacao { get; init; }
    public DateTime? DtPagamento { get; init; }
    public decimal? VlPago { get; init; }
}

public static class InstallmentStatus
{
    public const int Open = 1, Issued = 2, Paid = 3, Cancelled = 4;
    public static string Text(int status) => status switch { 1 => "Aberta", 2 => "Emitida", 3 => "Paga", 4 => "Cancelada", _ => "" };
}

public record GroupOption(int CdGrupo, string NrGrupo);

/// <summary>
/// Reads straight from the legacy database (parameterized; the legacy screen concatenates SQL).
/// Writes never happen here: they go through the legacy procedures (ADR-006).
/// </summary>
public class InstallmentQueries(IConfiguration cfg)
{
    public const int PageSize = 50;
    readonly string _connectionString = cfg.GetConnectionString("SGC")!;

    const string CotaLabel = "G.NR_GRUPO + '/' + RIGHT('000' + CAST(C.NR_COTA AS VARCHAR), 3) + '-' + CAST(C.NR_VERSAO AS VARCHAR)";
    const string Joins = """
        FROM TB_PARCELA P
       INNER JOIN TB_COTA C ON C.CD_COTA = P.CD_COTA
       INNER JOIN TB_GRUPO G ON G.CD_GRUPO = C.CD_GRUPO
       INNER JOIN TB_CONSORCIADO X ON X.CD_CONSORCIADO = C.CD_CONSORCIADO
       """;

    public async Task<IReadOnlyList<GroupOption>> GroupsAsync()
    {
        await using var cn = new SqlConnection(_connectionString);
        return (await cn.QueryAsync<GroupOption>("SELECT CD_GRUPO AS CdGrupo, NR_GRUPO AS NrGrupo FROM TB_GRUPO WHERE ID_SITUACAO = 2 ORDER BY NR_GRUPO")).ToList();
    }

    public async Task<(IReadOnlyList<InstallmentRow> Rows, int Total)> SearchAsync(InstallmentFilter f)
    {
        var where = "WHERE P.ID_SITUACAO <> 4";
        var p = new DynamicParameters();
        if (f.Group is not null) { where += " AND C.CD_GRUPO = @group"; p.Add("group", f.Group); }
        switch (f.Status)
        {
            case "V": where += " AND P.ID_SITUACAO = 2 AND dbo.FN_PROXIMO_DIA_UTIL(P.DT_VENCIMENTO) < CONVERT(VARCHAR(8), GETDATE(), 112)"; break;
            case "1": case "2": case "3": where += " AND P.ID_SITUACAO = @status"; p.Add("status", int.Parse(f.Status)); break;
        }
        if (f.From is not null) { where += " AND P.DT_VENCIMENTO >= @from"; p.Add("from", f.From); }
        if (f.To is not null) { where += " AND P.DT_VENCIMENTO <= @to"; p.Add("to", f.To); }
        if (!string.IsNullOrWhiteSpace(f.NossoNumero)) { where += " AND P.NR_NOSSO_NUMERO LIKE @nn"; p.Add("nn", "%" + f.NossoNumero.Trim() + "%"); }

        // the legacy screen caps the result at 2000 rows before paging; kept for parity
        var sql = $"""
            WITH X AS (
              SELECT TOP 2000 P.CD_PARCELA AS CdParcela, P.CD_COTA AS CdCota, {CotaLabel} AS DsCota,
                     X.NM_CONSORCIADO AS NmConsorciado, P.NR_PARCELA AS NrParcela, P.DT_VENCIMENTO AS DtVencimento,
                     P.NR_NOSSO_NUMERO AS NrNossoNumero, P.VL_PARCELA AS VlParcela, P.DT_PAGAMENTO AS DtPagamento, P.VL_PAGO AS VlPago,
                     CASE WHEN P.ID_SITUACAO = 2 AND dbo.FN_PROXIMO_DIA_UTIL(P.DT_VENCIMENTO) < GETDATE() - 1 THEN DATEDIFF(DAY, P.DT_VENCIMENTO, GETDATE())
                          WHEN P.ID_SITUACAO = 3 AND P.DT_PAGAMENTO > dbo.FN_PROXIMO_DIA_UTIL(P.DT_VENCIMENTO) THEN DATEDIFF(DAY, P.DT_VENCIMENTO, P.DT_PAGAMENTO) END AS QtDiasAtraso,
                     P.ID_SITUACAO AS IdSituacao,
                     ROW_NUMBER() OVER (ORDER BY P.DT_VENCIMENTO DESC, G.NR_GRUPO, C.NR_COTA) AS Ordem
                {Joins}
               {where}
               ORDER BY P.DT_VENCIMENTO DESC, G.NR_GRUPO, C.NR_COTA)
            SELECT * FROM (SELECT X.*, COUNT(*) OVER () AS Total FROM X) Y
             WHERE Y.Ordem BETWEEN @first AND @last
             ORDER BY Y.Ordem
            """;
        p.Add("first", (f.Page - 1) * PageSize + 1);
        p.Add("last", f.Page * PageSize);

        await using var cn = new SqlConnection(_connectionString);
        var rows = (await cn.QueryAsync<InstallmentRow>(sql, p)).ToList();
        return (rows, rows.Count > 0 ? rows[0].Total : 0);
    }

    public async Task<InstallmentForPayment?> ForPaymentAsync(int installmentId)
    {
        await using var cn = new SqlConnection(_connectionString);
        return await cn.QuerySingleOrDefaultAsync<InstallmentForPayment>($"""
            SELECT P.CD_PARCELA AS CdParcela, P.CD_COTA AS CdCota, {CotaLabel} AS DsCota, X.NM_CONSORCIADO AS NmConsorciado,
                   P.NR_PARCELA AS NrParcela, C.QT_PRAZO_COTA AS QtPrazoCota, P.NR_NOSSO_NUMERO AS NrNossoNumero,
                   P.DT_VENCIMENTO AS DtVencimento, P.VL_PARCELA AS VlParcela, P.ID_SITUACAO AS IdSituacao,
                   P.DT_PAGAMENTO AS DtPagamento, P.VL_PAGO AS VlPago
              {Joins}
             WHERE P.CD_PARCELA = @installmentId
            """, new { installmentId });
    }

    public async Task<int?> FindByNossoNumeroAsync(string nossoNumero)
    {
        await using var cn = new SqlConnection(_connectionString);
        return await cn.QuerySingleOrDefaultAsync<int?>("SELECT CD_PARCELA FROM TB_PARCELA WHERE NR_NOSSO_NUMERO = @nossoNumero", new { nossoNumero = nossoNumero.Trim() });
    }
}
