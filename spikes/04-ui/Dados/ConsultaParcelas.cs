using Dapper;
using Microsoft.Data.SqlClient;

namespace Poc04.Ui.Dados;

/// <summary>Filtros da consulta de parcelas: mesmos nomes de parâmetro da tela legada (Parcelas.aspx?grupo=4).</summary>
public record FiltroParcelas(int? Grupo, string Situacao, DateTime? De, DateTime? Ate, string? NossoNumero, int Pagina = 1)
{
    public static readonly FiltroParcelas Padrao = new(null, "V", null, null, null);
}

public class LinhaParcela
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
    public string Situacao => IdSituacao switch { 1 => "Aberta", 2 => "Emitida", 3 => "Paga", 4 => "Cancelada", _ => "" };
}

public record Grupo(int CdGrupo, string NrGrupo);

/// <summary>
/// Mesma regra de filtro da Cobranca/Parcelas.aspx, mas parametrizada (o legado concatena o SQL).
/// Leitura direta no banco do legado; escrita, quando houver, só pelas procedures do legado.
/// </summary>
public class ConsultaParcelas(IConfiguration cfg)
{
    public const int TamanhoPagina = 50;
    readonly string _conexao = cfg.GetConnectionString("SGC")!;

    public async Task<IReadOnlyList<Grupo>> GruposAsync()
    {
        await using var cn = new SqlConnection(_conexao);
        return (await cn.QueryAsync<Grupo>("SELECT CD_GRUPO AS CdGrupo, NR_GRUPO AS NrGrupo FROM TB_GRUPO WHERE ID_SITUACAO = 2 ORDER BY NR_GRUPO")).ToList();
    }

    public async Task<(IReadOnlyList<LinhaParcela> Linhas, int Total)> PesquisarAsync(FiltroParcelas f)
    {
        var where = "WHERE P.ID_SITUACAO <> 4";
        var p = new DynamicParameters();
        if (f.Grupo is not null) { where += " AND C.CD_GRUPO = @grupo"; p.Add("grupo", f.Grupo); }
        switch (f.Situacao)
        {
            case "V": where += " AND P.ID_SITUACAO = 2 AND dbo.FN_PROXIMO_DIA_UTIL(P.DT_VENCIMENTO) < CONVERT(VARCHAR(8), GETDATE(), 112)"; break;
            case "1": case "2": case "3": where += " AND P.ID_SITUACAO = @sit"; p.Add("sit", int.Parse(f.Situacao)); break;
        }
        if (f.De is not null) { where += " AND P.DT_VENCIMENTO >= @de"; p.Add("de", f.De); }
        if (f.Ate is not null) { where += " AND P.DT_VENCIMENTO <= @ate"; p.Add("ate", f.Ate); }
        if (!string.IsNullOrWhiteSpace(f.NossoNumero)) { where += " AND P.NR_NOSSO_NUMERO LIKE @nn"; p.Add("nn", "%" + f.NossoNumero.Trim() + "%"); }

        // o legado limita a 2000 linhas antes de paginar; mantido para a paridade
        var sql = $@"
WITH X AS (
  SELECT TOP 2000 P.CD_PARCELA AS CdParcela, P.CD_COTA AS CdCota,
         G.NR_GRUPO + '/' + RIGHT('000' + CAST(C.NR_COTA AS VARCHAR), 3) + '-' + CAST(C.NR_VERSAO AS VARCHAR) AS DsCota,
         X.NM_CONSORCIADO AS NmConsorciado, P.NR_PARCELA AS NrParcela, P.DT_VENCIMENTO AS DtVencimento,
         P.NR_NOSSO_NUMERO AS NrNossoNumero, P.VL_PARCELA AS VlParcela, P.DT_PAGAMENTO AS DtPagamento, P.VL_PAGO AS VlPago,
         CASE WHEN P.ID_SITUACAO = 2 AND dbo.FN_PROXIMO_DIA_UTIL(P.DT_VENCIMENTO) < GETDATE() - 1 THEN DATEDIFF(DAY, P.DT_VENCIMENTO, GETDATE())
              WHEN P.ID_SITUACAO = 3 AND P.DT_PAGAMENTO > dbo.FN_PROXIMO_DIA_UTIL(P.DT_VENCIMENTO) THEN DATEDIFF(DAY, P.DT_VENCIMENTO, P.DT_PAGAMENTO) END AS QtDiasAtraso,
         P.ID_SITUACAO AS IdSituacao,
         ROW_NUMBER() OVER (ORDER BY P.DT_VENCIMENTO DESC, G.NR_GRUPO, C.NR_COTA) AS Ordem
    FROM TB_PARCELA P
   INNER JOIN TB_COTA C ON C.CD_COTA = P.CD_COTA
   INNER JOIN TB_GRUPO G ON G.CD_GRUPO = C.CD_GRUPO
   INNER JOIN TB_CONSORCIADO X ON X.CD_CONSORCIADO = C.CD_CONSORCIADO
   {where}
   ORDER BY P.DT_VENCIMENTO DESC, G.NR_GRUPO, C.NR_COTA)
SELECT * FROM (SELECT X.*, COUNT(*) OVER () AS Total FROM X) Y
 WHERE Y.Ordem BETWEEN @ini AND @fim
 ORDER BY Y.Ordem";
        p.Add("ini", (f.Pagina - 1) * TamanhoPagina + 1);
        p.Add("fim", f.Pagina * TamanhoPagina);

        await using var cn = new SqlConnection(_conexao);
        var linhas = (await cn.QueryAsync<LinhaParcela>(sql, p)).ToList();
        return (linhas, linhas.Count > 0 ? linhas[0].Total : 0);
    }
}
