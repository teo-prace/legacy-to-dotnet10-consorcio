using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Playwright;
using Poc06.Escrita;
using Sgc.Tests.Infra;
using Xunit.Abstractions;

namespace Sgc.Tests.Spikes;

/// <summary>
/// Cópia do banco só para a POC-06 (SGC_POC06): os testes gravam baixas, e o banco SGC do legado não pode mudar.
/// Cada teste restaura a cópia a partir de um backup do SGC tirado no início.
/// </summary>
public sealed class BancoPoc06 : IDisposable
{
    public const string Nome = "SGC_POC06";
    public const string Servidor = "(localdb)\\MSSQLLocalDB";
    public static string Conexao(string banco = Nome) => $"Data Source={Servidor};Initial Catalog={banco};Integrated Security=True;TrustServerCertificate=True;Max Pool Size=200";
    readonly string _bak = Path.Combine(Path.GetTempPath(), "sgc-poc06.bak");
    readonly string _pasta = Path.Combine(Path.GetTempPath(), "sgc-poc06");

    public BancoPoc06()
    {
        Directory.CreateDirectory(_pasta);
        using var cn = new SqlConnection(Conexao("master"));
        cn.Execute($"BACKUP DATABASE SGC TO DISK = @bak WITH COPY_ONLY, INIT, FORMAT", new { bak = _bak }, commandTimeout: 300);
    }

    /// <summary>Volta a cópia ao estado do SGC e emite as cobranças de 12/2026 e 01/2027 (mais parcelas em aberto).</summary>
    public void Restaurar()
    {
        SqlConnection.ClearAllPools();
        using var cn = new SqlConnection(Conexao("master"));
        var arquivos = cn.Query($"RESTORE FILELISTONLY FROM DISK = @bak", new { bak = _bak })
                         .Select(f => $"MOVE '{(string)f.LogicalName}' TO '{Path.Combine(_pasta, Nome + ((string)f.Type == "L" ? "_log.ldf" : ".mdf"))}'");
        cn.Execute($"IF DB_ID('{Nome}') IS NOT NULL ALTER DATABASE {Nome} SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
        cn.Execute($"RESTORE DATABASE {Nome} FROM DISK = @bak WITH REPLACE, {string.Join(", ", arquivos)}", new { bak = _bak }, commandTimeout: 300);
        cn.Execute($"ALTER DATABASE {Nome} SET MULTI_USER");
        using var db = new SqlConnection(Conexao());
        db.Execute("EXEC SP_EMITE_COBRANCA 202612, NULL, 1; EXEC SP_EMITE_COBRANCA 202701, NULL, 1", commandTimeout: 300);
    }

    public void Dispose()
    {
        SqlConnection.ClearAllPools();
        using var cn = new SqlConnection(Conexao("master"));
        cn.Execute($"IF DB_ID('{Nome}') IS NOT NULL BEGIN ALTER DATABASE {Nome} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE {Nome}; END");
    }
}

[CollectionDefinition("poc06")]
public class ColecaoPoc06 : ICollectionFixture<BancoPoc06>, ICollectionFixture<BrowserFixture> { }

[Collection("poc06")]
[Trait("Spike", "05")]
public class Spike05SharedDatabaseTests
{
    const int PortaLegadoCopia = 8096;
    readonly BancoPoc06 _banco;
    readonly BrowserFixture _nav;
    readonly ITestOutputHelper _saida;
    public Spike05SharedDatabaseTests(BancoPoc06 banco, BrowserFixture nav, ITestOutputHelper saida) { _banco = banco; _nav = nav; _saida = saida; }

    static SqlConnection Abrir() { var c = new SqlConnection(BancoPoc06.Conexao()); c.Open(); return c; }

    sealed record Parcela(int CdParcela, int CdCota, decimal VlParcela, decimal PcFundoComum);

    static List<Parcela> Emitidas(string filtroCota = "1 = 1") =>
        Abrir().Query<Parcela>($@"SELECT P.CD_PARCELA AS CdParcela, P.CD_COTA AS CdCota, P.VL_PARCELA AS VlParcela, P.PC_FUNDO_COMUM AS PcFundoComum
                                     FROM TB_PARCELA P JOIN TB_COTA C ON C.CD_COTA = P.CD_COTA
                                    WHERE P.ID_SITUACAO = 2 AND {filtroCota} ORDER BY P.CD_PARCELA").ToList();

    // ------------------------------------------------------------------------------------------------
    [Fact(DisplayName = "05-1 · contract: SP_BAIXA_PARCELA has the signature the new screen expects")]
    public void ContratoDaProcedure()
    {
        // A tela nova depende da procedure do legado. Se alguém do legado mudar a assinatura,
        // este teste quebra no CI antes de quebrar em produção.
        _banco.Restaurar();
        var esperado = new[]
        {
            "@CD_PARCELA int", "@DT_PAGAMENTO datetime", "@VL_PAGO money", "@VL_MULTA money", "@VL_JUROS money",
            "@ID_ORIGEM tinyint", "@CD_USUARIO int", "@CD_RETORNO_DETALHE int",
        };
        var real = Abrir().Query<string>(@"SELECT P.name + ' ' + T.name FROM sys.parameters P JOIN sys.types T ON T.user_type_id = P.user_type_id
                                            WHERE P.object_id = OBJECT_ID('SP_BAIXA_PARCELA') ORDER BY P.parameter_id").ToArray();
        Assert.Equal(esperado, real);

        // e as consultas da tela nova compilam contra o esquema atual (colunas renomeadas/removidas aparecem aqui)
        var consulta = "SELECT P.CD_PARCELA, P.CD_COTA, P.NR_PARCELA, P.DT_VENCIMENTO, P.NR_NOSSO_NUMERO, P.VL_PARCELA, P.DT_PAGAMENTO, P.VL_PAGO, P.ID_SITUACAO, " +
                       "C.NR_COTA, C.NR_VERSAO, G.NR_GRUPO, X.NM_CONSORCIADO FROM TB_PARCELA P JOIN TB_COTA C ON C.CD_COTA = P.CD_COTA " +
                       "JOIN TB_GRUPO G ON G.CD_GRUPO = C.CD_GRUPO JOIN TB_CONSORCIADO X ON X.CD_CONSORCIADO = C.CD_CONSORCIADO";
        var erros = Abrir().Query<string>("SELECT error_message FROM sys.dm_exec_describe_first_result_set(@q, NULL, 0) WHERE error_message IS NOT NULL", new { q = consulta }).ToList();
        Assert.Empty(erros);
    }

    // ------------------------------------------------------------------------------------------------
    [Fact(DisplayName = "05-2 · 20 concurrent payments of the same installment: exactly one is posted")]
    public async Task CorridaNaMesmaParcela()
    {
        _banco.Restaurar();
        var alvo = Emitidas().First();
        var antes = Abrir().ExecuteScalar<decimal>("SELECT PC_AMORTIZADO FROM TB_COTA WHERE CD_COTA = @c", new { c = alvo.CdCota });
        var gw = new BaixaParcelas(BancoPoc06.Conexao());
        var largada = new TaskCompletionSource();
        var tarefas = Enumerable.Range(0, 20).Select(async _ => { await largada.Task; return await gw.BaixarAsync(alvo.CdParcela, DateTime.Today, alvo.VlParcela, 0, 0, 1); }).ToList();
        largada.SetResult();
        var r = await Task.WhenAll(tarefas);

        foreach (var g in r.GroupBy(x => (x.Status, x.Mensagem))) _saida.WriteLine($"{g.Count(),2} x {g.Key.Status}: {g.Key.Mensagem}");
        Assert.Equal(1, r.Count(x => x.Status == StatusBaixa.Baixada));
        Assert.Equal(19, r.Count(x => x.Status == StatusBaixa.Conflito));

        using var cn = Abrir();
        Assert.Equal(3, cn.ExecuteScalar<int>("SELECT COUNT(*) FROM TB_GRUPO_MOVIMENTO WHERE CD_PARCELA = @p", new { p = alvo.CdParcela }));   // FC, FR, TA uma vez só
        Assert.Equal(antes + alvo.PcFundoComum, cn.ExecuteScalar<decimal>("SELECT PC_AMORTIZADO FROM TB_COTA WHERE CD_COTA = @c", new { c = alvo.CdCota }));
    }

    // ------------------------------------------------------------------------------------------------
    [Fact(DisplayName = "05-3 · operator on the legacy screen and the new screen pay the same installment: the second is warned, no double payment")]
    public async Task LegadoENovoNaMesmaParcela()
    {
        _banco.Restaurar();
        var url = await SubirLegadoNaCopiaAsync();
        var alvo = Emitidas().First();

        var p = await _nav.NewPageAsync();
        p.Dialog += async (_, d) => await d.AcceptAsync();   // "Confirma a baixa da parcela?" (o Playwright recusa por padrão)
        await BrowserFixture.LoginAsync(p, url);
        // 1. o operador abre a parcela na tela de baixa do legado e calcula
        await p.GotoAsync($"{url}/Cobranca/Baixa.aspx?id={alvo.CdParcela}");
        await p.FillAsync("#ctl00_cphConteudo_txtDataPagamento", DateTime.Today.ToString("dd/MM/yyyy"));
        await Task.WhenAll(p.WaitForLoadStateAsync(LoadState.NetworkIdle), p.ClickAsync("#ctl00_cphConteudo_btnCalcular"));
        // 2. enquanto isso, outra pessoa baixa a mesma parcela pela tela nova
        var novo = await new BaixaParcelas(BancoPoc06.Conexao()).BaixarAsync(alvo.CdParcela, DateTime.Today, alvo.VlParcela, 0, 0, 2);
        Assert.Equal(StatusBaixa.Baixada, novo.Status);
        // 3. o operador confirma no legado
        await Task.WhenAll(p.WaitForLoadStateAsync(LoadState.NetworkIdle), p.ClickAsync("#ctl00_cphConteudo_btnBaixar"));
        var msg = await p.InnerTextAsync("#ctl00_cphConteudo_lblMensagem");
        _saida.WriteLine($"tela legada mostrou: {msg}");
        Assert.Contains("paga", msg, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, Abrir().ExecuteScalar<int>("SELECT COUNT(*) FROM TB_GRUPO_MOVIMENTO WHERE CD_PARCELA = @p", new { p = alvo.CdParcela }));

        // e o inverso: o legado grava primeiro, a tela nova recebe Conflito
        var outra = Emitidas().First();
        await p.GotoAsync($"{url}/Cobranca/Baixa.aspx?id={outra.CdParcela}");
        await p.FillAsync("#ctl00_cphConteudo_txtDataPagamento", DateTime.Today.ToString("dd/MM/yyyy"));
        await Task.WhenAll(p.WaitForLoadStateAsync(LoadState.NetworkIdle), p.ClickAsync("#ctl00_cphConteudo_btnCalcular"));
        await Task.WhenAll(p.WaitForLoadStateAsync(LoadState.NetworkIdle), p.ClickAsync("#ctl00_cphConteudo_btnBaixar"));
        Assert.Contains("baixada", await p.InnerTextAsync("#ctl00_cphConteudo_lblMensagem"), StringComparison.OrdinalIgnoreCase);
        var depois = await new BaixaParcelas(BancoPoc06.Conexao()).BaixarAsync(outra.CdParcela, DateTime.Today, outra.VlParcela, 0, 0, 2);
        _saida.WriteLine($"tela nova recebeu: {depois.Status} / {depois.Mensagem}");
        Assert.Equal(StatusBaixa.Conflito, depois.Status);
        TestEnvironment.Stop(PortaLegadoCopia);
    }

    /// <summary>Uma cópia do site legado apontando para SGC_POC06, para o teste não gravar no banco de verdade.</summary>
    async Task<string> SubirLegadoNaCopiaAsync()
    {
        var destino = Path.Combine(Path.GetTempPath(), "sgc-web-poc06");
        if (Directory.Exists(destino)) Directory.Delete(destino, true);
        CopiarPasta(Path.Combine(TestEnvironment.Root, "legacy", "src", "SGC.Web"), destino);
        var cfg = Path.Combine(destino, "web.config");
        File.WriteAllText(cfg, File.ReadAllText(cfg).Replace("Initial Catalog=SGC;Integrated", $"Initial Catalog={BancoPoc06.Nome};Integrated"));
        TestEnvironment.Stop(PortaLegadoCopia);
        await TestEnvironment.StartIisExpressAsync(destino, PortaLegadoCopia);
        return $"http://localhost:{PortaLegadoCopia}";
    }

    static void CopiarPasta(string de, string para)
    {
        foreach (var d in Directory.GetDirectories(de, "*", SearchOption.AllDirectories)) Directory.CreateDirectory(d.Replace(de, para));
        Directory.CreateDirectory(para);
        foreach (var f in Directory.GetFiles(de, "*", SearchOption.AllDirectories)) File.Copy(f, f.Replace(de, para), true);
    }

    // ------------------------------------------------------------------------------------------------
    [Fact(DisplayName = "05-4 · concurrent payments of different installments of one contemplated quota: deadlock in the procedure")]
    public async Task DeadlockNaMesmaCota()
    {
        const int rodadas = 5;
        int deadlocksSemRetentativa = 0, baixadasSemRetentativa = 0, totalSemRetentativa = 0;
        int baixadasComRetentativa = 0, totalComRetentativa = 0, retentativas = 0;

        for (var rodada = 0; rodada < rodadas; rodada++)
            foreach (var comRetentativa in new[] { false, true })
            {
                _banco.Restaurar();
                VoltarProcedureParaVersaoAntiga();
                // cotas contempladas (situação 2) com 2 ou mais parcelas emitidas: a quitação lê as parcelas da cota
                var alvos = Emitidas("C.ID_SITUACAO = 2 AND P.CD_COTA IN (SELECT CD_COTA FROM TB_PARCELA WHERE ID_SITUACAO = 2 GROUP BY CD_COTA HAVING COUNT(*) > 1)");
                var gw = new BaixaParcelas(BancoPoc06.Conexao(), maxTentativas: comRetentativa ? 5 : 1);
                var largada = new TaskCompletionSource();
                var tarefas = alvos.Select(async a =>
                {
                    await largada.Task;
                    try { return await gw.BaixarAsync(a.CdParcela, DateTime.Today, a.VlParcela, 0, 0, 1); }
                    catch (SqlException e) when (e.Number == 1205) { return new ResultadoBaixa(StatusBaixa.Recusada, "deadlock", 1); }
                }).ToList();
                largada.SetResult();
                var r = await Task.WhenAll(tarefas);
                if (comRetentativa)
                {
                    totalComRetentativa += r.Length;
                    baixadasComRetentativa += r.Count(x => x.Status == StatusBaixa.Baixada);
                    retentativas += r.Sum(x => x.Tentativas - 1);
                }
                else
                {
                    totalSemRetentativa += r.Length;
                    baixadasSemRetentativa += r.Count(x => x.Status == StatusBaixa.Baixada);
                    deadlocksSemRetentativa += r.Count(x => x.Mensagem == "deadlock");
                }
                // invariante: cada parcela baixada tem os 3 lançamentos; nenhuma parcela pela metade
                Assert.Equal(0, Abrir().ExecuteScalar<int>(@"SELECT COUNT(*) FROM TB_PARCELA P WHERE P.ID_SITUACAO = 3 AND P.DT_PAGAMENTO = CAST(GETDATE() AS DATE)
                                                              AND (SELECT COUNT(*) FROM TB_GRUPO_MOVIMENTO M WHERE M.CD_PARCELA = P.CD_PARCELA) <> 3"));
            }

        _saida.WriteLine($"{rodadas} rodadas, {totalSemRetentativa / rodadas} baixas simultâneas por rodada (parcelas de cotas contempladas)");
        _saida.WriteLine($"sem nova tentativa (como a tela legada): {baixadasSemRetentativa}/{totalSemRetentativa} baixadas, {deadlocksSemRetentativa} vítimas de deadlock");
        _saida.WriteLine($"com nova tentativa (tela nova): {baixadasComRetentativa}/{totalComRetentativa} baixadas, {retentativas} novas tentativas");
        Assert.True(deadlocksSemRetentativa > 0, "esperava reproduzir o deadlock da procedure");
        Assert.Equal(totalComRetentativa, baixadasComRetentativa);
    }

    /// <summary>
    /// The fix is in legacy/database/procs/SP_BAIXA_PARCELA.sql (the "[MIGRACAO 2026-11 ...]" block). To keep
    /// proving the problem, 05-4 removes that block on the database copy and goes back to the 2016 version.
    /// </summary>
    static void VoltarProcedureParaVersaoAntiga()
    {
        using var cn = Abrir();
        var atual = cn.ExecuteScalar<string>("SELECT OBJECT_DEFINITION(OBJECT_ID('SP_BAIXA_PARCELA'))")!;
        var antiga = System.Text.RegularExpressions.Regex.Replace(atual, @"-- \[MIGRACAO 2026-11 - inicio\].*?-- \[MIGRACAO 2026-11 - fim\]\r?\n",
                                                                   "", System.Text.RegularExpressions.RegexOptions.Singleline);
        Assert.NotEqual(atual, antiga);
        cn.Execute(antiga.Replace("CREATE PROCEDURE", "ALTER PROCEDURE"));
    }

    [Fact(DisplayName = "05-4b · fixed procedure (locks the quota first in the transaction): no deadlock, even without retry")]
    public async Task CorrecaoDoDeadlockNaProcedure()
    {
        const int rodadas = 5;
        int total = 0, baixadas = 0, deadlocks = 0;
        for (var rodada = 0; rodada < rodadas; rodada++)
        {
            _banco.Restaurar();
            // a procedure do repositório já tem a correção (legacy/database/procs/SP_BAIXA_PARCELA.sql)
            Assert.Contains("[MIGRACAO 2026-11 - inicio]", Abrir().ExecuteScalar<string>("SELECT OBJECT_DEFINITION(OBJECT_ID('SP_BAIXA_PARCELA'))"));
            var alvos = Emitidas("C.ID_SITUACAO = 2 AND P.CD_COTA IN (SELECT CD_COTA FROM TB_PARCELA WHERE ID_SITUACAO = 2 GROUP BY CD_COTA HAVING COUNT(*) > 1)");
            var gw = new BaixaParcelas(BancoPoc06.Conexao(), maxTentativas: 1);
            var largada = new TaskCompletionSource();
            var tarefas = alvos.Select(async a =>
            {
                await largada.Task;
                try { return await gw.BaixarAsync(a.CdParcela, DateTime.Today, a.VlParcela, 0, 0, 1); }
                catch (SqlException e) when (e.Number == 1205) { return new ResultadoBaixa(StatusBaixa.Recusada, "deadlock", 1); }
            }).ToList();
            largada.SetResult();
            var r = await Task.WhenAll(tarefas);
            total += r.Length; baixadas += r.Count(x => x.Status == StatusBaixa.Baixada); deadlocks += r.Count(x => x.Mensagem == "deadlock");
        }
        _saida.WriteLine($"procedure corrigida, sem nova tentativa: {baixadas}/{total} baixadas, {deadlocks} vítimas de deadlock");
        Assert.Equal(0, deadlocks);
        Assert.Equal(total, baixadas);
    }

    // ------------------------------------------------------------------------------------------------
    [Fact(DisplayName = "05-5 · grid read during a payment: blocks as the database is (READ COMMITTED); doesn't with RCSI")]
    public async Task LeituraDuranteBaixa()
    {
        _banco.Restaurar();
        var alvo = Emitidas().First();
        const string grade = "SELECT COUNT(*) FROM TB_PARCELA P JOIN TB_COTA C ON C.CD_COTA = P.CD_COTA WHERE P.ID_SITUACAO <> 4";

        async Task<(bool Bloqueou, long Ms)> MedirAsync()
        {
            using var escrita = Abrir();
            using var tx = escrita.BeginTransaction();
            // uma baixa parada no meio da transação (o UPDATE da procedure, sem o COMMIT)
            escrita.Execute("UPDATE TB_PARCELA SET ID_SITUACAO = 3 WHERE CD_PARCELA = @p", new { p = alvo.CdParcela }, tx);
            using var leitura = Abrir();
            var sw = Stopwatch.StartNew();
            try { await leitura.ExecuteScalarAsync<int>(grade, commandTimeout: 3); return (false, sw.ElapsedMilliseconds); }
            catch (SqlException e) when (e.Number == -2) { return (true, sw.ElapsedMilliseconds); }
            finally { tx.Rollback(); }
        }

        var rc = await MedirAsync();
        _saida.WriteLine($"READ COMMITTED (como o legado está): {(rc.Bloqueou ? "BLOQUEOU" : "não bloqueou")} ({rc.Ms} ms)");

        SqlConnection.ClearAllPools();
        using (var m = new SqlConnection(BancoPoc06.Conexao("master")))
            m.Execute($"ALTER DATABASE {BancoPoc06.Nome} SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE");
        var rcsi = await MedirAsync();
        _saida.WriteLine($"READ COMMITTED SNAPSHOT: {(rcsi.Bloqueou ? "BLOQUEOU" : "não bloqueou")} ({rcsi.Ms} ms)");

        Assert.True(rc.Bloqueou);
        Assert.False(rcsi.Bloqueou);
    }
}
