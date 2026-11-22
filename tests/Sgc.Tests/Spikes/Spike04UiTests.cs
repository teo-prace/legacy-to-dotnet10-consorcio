using System.Diagnostics;
using Microsoft.Playwright;
using Sgc.Tests.Infra;
using Xunit.Abstractions;

namespace Sgc.Tests.Spikes;

/// <summary>
/// POC-04 · a mesma tela (consulta de parcelas) em Razor Pages, Blazor SSR estático e Blazor Interactive Server.
/// Fachada em http://localhost:8105.
/// </summary>
[Collection("browser")]
[Trait("Spike", "04")]
public class Spike04UiTests : IAsyncLifetime
{
    const string Fachada = "http://localhost:8105";
    static readonly string[] Tecnologias = { "rp", "ssr", "int" };
    readonly BrowserFixture _nav;
    readonly ITestOutputHelper _saida;

    public Spike04UiTests(BrowserFixture nav, ITestOutputHelper saida) { _nav = nav; _saida = saida; }

    public async Task InitializeAsync()
    {
        await TestEnvironment.EnsureLegacyAsync();
        if (!await TestEnvironment.RespondsAsync(Fachada + "/novo/ping"))
            await TestEnvironment.StartSpikeAsync("04-ui", "Spike04.Ui.dll", 8105);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    static async Task<List<string>> NossosNumerosAsync(IPage p, string seletorCelula) =>
        (await p.Locator(seletorCelula).AllInnerTextsAsync()).Select(t => t.Trim()).Where(t => t.Length > 0).ToList();

    static async Task EsperarInterativoAsync(IPage p)
    {
        // o circuito do Blazor Server está conectado quando o botão já tem o manipulador do Blazor
        await p.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(1000);
    }

    /// <summary>Filtra na tela legada e devolve os nossos números da página 1.</summary>
    async Task<List<string>> LegadoAsync(IPage p, string grupo, string situacao, string nn = "")
    {
        await p.GotoAsync(Fachada + "/Cobranca/Parcelas.aspx");
        await p.SelectOptionAsync("#ctl00_cphConteudo_ddlGrupo", grupo);
        await p.SelectOptionAsync("#ctl00_cphConteudo_ddlSituacao", situacao);
        await p.FillAsync("#ctl00_cphConteudo_txtNossoNumero", nn);
        await Task.WhenAll(p.WaitForLoadStateAsync(LoadState.NetworkIdle), p.ClickAsync("#ctl00_cphConteudo_btnPesquisar"));
        // só as linhas de dados (o pager do GridView é uma linha com tabela aninhada)
        return await NossosNumerosAsync(p, "#ctl00_cphConteudo_gvParcelas > tbody > tr:not(.rodapeGrid) > td:nth-child(5)");
    }

    async Task<List<string>> NovoAsync(IPage p, string tec, string grupo, string situacao, string nn = "")
    {
        if (situacao == "" && tec == "int") situacao = "T";   // na tela nova, "(todas)" tem valor explícito
        if (tec == "int")
        {
            await p.GotoAsync($"{Fachada}/novo/int/parcelas");
            await EsperarInterativoAsync(p);
            await p.SelectOptionAsync("select[name=grupo]", grupo);
            await p.SelectOptionAsync("select[name=situacao]", situacao);
            await p.FillAsync("input[name=nn]", nn);
            await p.ClickAsync("#pesquisar");
            await Task.Delay(1500);
        }
        else
            await p.GotoAsync($"{Fachada}/novo/{tec}/parcelas?grupo={grupo}&situacao={situacao}&nn={nn}");
        return await NossosNumerosAsync(p, "#grade td.nn");
    }

    [Fact(DisplayName = "04-1 · parity: the three versions return the same rows, in the same order, as the legacy screen")]
    public async Task Paridade()
    {
        var p = await _nav.NewPageAsync();
        await BrowserFixture.LoginAsync(p, Fachada);
        var cenarios = new[] { ("4", "V", ""), ("4", "3", ""), ("4", "2", ""), ("4", "", "00000022"), ("1", "1", "") };
        foreach (var (grupo, sit, nn) in cenarios)
        {
            var esperado = await LegadoAsync(p, grupo, sit, nn);
            foreach (var tec in Tecnologias)
            {
                var obtido = await NovoAsync(p, tec, grupo, sit, nn);
                _saida.WriteLine($"grupo={grupo} situação='{sit}' nn='{nn}' {tec}: {obtido.Count} linhas (legado {esperado.Count}) {(obtido.SequenceEqual(esperado) ? "IGUAL" : "DIFERENTE")}");
                Assert.Equal(esperado, obtido);
            }
        }
    }

    [Fact(DisplayName = "04-2 · deep link, refresh and Back after filtering and paging")]
    public async Task LinkDiretoF5Voltar()
    {
        foreach (var tec in Tecnologias)
        {
            var p = await _nav.NewPageAsync();
            await BrowserFixture.LoginAsync(p, Fachada);
            await p.GotoAsync($"{Fachada}/novo/{tec}/parcelas");
            if (tec == "int") await EsperarInterativoAsync(p);
            await p.SelectOptionAsync("select[name=situacao]", "3");   // pagas: várias páginas
            await p.ClickAsync("#pesquisar");
            await p.WaitForSelectorAsync("#paginas");
            var primeira = await NossosNumerosAsync(p, "#grade td.nn");
            await p.ClickAsync("#paginas a:text-is('2')");
            await Task.Delay(1500);
            var segunda = await NossosNumerosAsync(p, "#grade td.nn");
            var urlNaPagina2 = p.Url;

            await p.ReloadAsync();
            if (tec == "int") await EsperarInterativoAsync(p);
            var depoisF5 = await NossosNumerosAsync(p, "#grade td.nn");
            var f5Manteve = depoisF5.SequenceEqual(segunda);

            await p.GotoAsync(urlNaPagina2);   // "link direto" para onde o usuário estava
            if (tec == "int") await EsperarInterativoAsync(p);
            var link = await NossosNumerosAsync(p, "#grade td.nn");
            var linkManteve = link.SequenceEqual(segunda);

            _saida.WriteLine($"{tec}: URL na página 2 = {urlNaPagina2.Replace(Fachada, "")}; F5 manteve filtro+página = {f5Manteve}; link direto reproduz = {linkManteve}");
            Assert.NotEqual(primeira, segunda);
            if (tec != "int") { Assert.True(f5Manteve); Assert.True(linkManteve); }
            else { Assert.False(f5Manteve); Assert.False(linkManteve); }   // estado só no circuito
        }
    }

    [Fact(DisplayName = "04-3 · without JavaScript: Razor Pages and SSR filter; Interactive shows only the first load")]
    public async Task SemJavaScript()
    {
        var ctx = await _nav.Browser.NewContextAsync(new() { JavaScriptEnabled = false });
        var p = await ctx.NewPageAsync();
        // login sem JS: o form do legado é um POST comum
        await p.GotoAsync(Fachada + "/Login.aspx");
        await p.FillAsync("#txtLogin", "msouza");
        await p.FillAsync("#txtSenha", "cecons123");
        await p.ClickAsync("#btnEntrar");
        foreach (var tec in Tecnologias)
        {
            await p.GotoAsync($"{Fachada}/novo/{tec}/parcelas?grupo=4");
            var antes = await NossosNumerosAsync(p, "#grade td.nn");
            await p.SelectOptionAsync("select[name=situacao]", "3");
            await p.ClickAsync("#pesquisar");
            await Task.Delay(1500);
            var depois = await NossosNumerosAsync(p, "#grade td.nn");
            var filtrou = !depois.SequenceEqual(antes) && depois.Count > 0;
            _saida.WriteLine($"{tec} sem JS: primeira carga {antes.Count} linhas; filtrar por pagas funcionou = {filtrou}");
            if (tec == "int") Assert.False(filtrou); else Assert.True(filtrou);
        }
    }

    [Fact(DisplayName = "04-4 · first load cost: bytes, requests and connections")]
    public async Task CustoPrimeiraCarga()
    {
        foreach (var tec in Tecnologias)
        {
            var ctx = await _nav.Browser.NewContextAsync();
            var p = await ctx.NewPageAsync();
            await BrowserFixture.LoginAsync(p, Fachada);
            long bytes = 0; var reqs = 0; var sockets = 0;
            p.Response += async (_, r) => { reqs++; try { bytes += (await r.BodyAsync()).Length; } catch { } };
            p.WebSocket += (_, _) => sockets++;
            await p.GotoAsync($"{Fachada}/novo/{tec}/parcelas?grupo=4");
            await p.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1000);
            _saida.WriteLine($"{tec}: {reqs} requisições, {bytes / 1024.0:N1} KB, websockets abertos {sockets}");
            await ctx.CloseAsync();
        }
    }

    [Fact(DisplayName = "04-5 · Interactive: prerendering queries the database twice per load")]
    public async Task ConsultasDuplicadas()
    {
        using var http = TestEnvironment.Client();
        var p = await _nav.NewPageAsync();
        await BrowserFixture.LoginAsync(p, Fachada);
        var antes = long.Parse(await http.GetStringAsync(Fachada + "/novo/diag/consultas-int"));
        await p.GotoAsync(Fachada + "/novo/int/parcelas?grupo=4");
        await EsperarInterativoAsync(p);
        var depois = long.Parse(await http.GetStringAsync(Fachada + "/novo/diag/consultas-int"));
        _saida.WriteLine($"consultas ao banco numa carga da tela interativa: {depois - antes}");
        Assert.Equal(2, depois - antes);
    }

    [Fact(DisplayName = "04-6 · server restart (deploy): SSR and Razor Pages carry on; Interactive loses the circuit")]
    public async Task ReinicioDoServidor()
    {
        const int porta = 8115;
        var url = await TestEnvironment.StartSpikeAsync("04-ui", "Spike04.Ui.dll", porta);
        var resultados = new Dictionary<string, bool>();
        try
        {
            // o usuário filtra (pagas) e vai para a página 2 ANTES do deploy
            var paginas = new Dictionary<string, (IPage Pagina, List<string> Antes)>();
            foreach (var tec in Tecnologias)
            {
                var p = await _nav.NewPageAsync();
                await BrowserFixture.LoginAsync(p, url);
                await p.GotoAsync($"{url}/novo/{tec}/parcelas?grupo=4");
                if (tec == "int") await EsperarInterativoAsync(p);
                await p.SelectOptionAsync("select[name=situacao]", "3");
                await p.ClickAsync("#pesquisar");
                await p.WaitForSelectorAsync("#paginas");
                await p.ClickAsync("#paginas a:text-is('2')");
                await Task.Delay(1500);
                paginas[tec] = (p, await NossosNumerosAsync(p, "#grade td.nn"));
            }

            TestEnvironment.Stop(porta);                      // "deploy"
            await TestEnvironment.StartSpikeAsync("04-ui", "Spike04.Ui.dll", porta);
            await Task.Delay(6000);                        // tempo para o Blazor tentar reconectar

            // depois do deploy, o usuário continua de onde estava: vai para a página 3
            foreach (var (tec, (p, antes)) in paginas)
            {
                var telaAindaMostra = (await NossosNumerosAsync(p, "#grade td.nn")).SequenceEqual(antes);
                var temPagina3 = await p.Locator("#paginas a:text-is('3')").CountAsync() > 0;
                if (temPagina3) { await p.ClickAsync("#paginas a:text-is('3')"); await Task.Delay(2000); }
                var linhas = await NossosNumerosAsync(p, "#grade td.nn");
                resultados[tec] = temPagina3 && linhas.Count == 50 && !linhas.SequenceEqual(antes);
                _saida.WriteLine($"{tec}: depois do deploy, a tela ainda mostrava a página 2 de pagas = {telaAindaMostra}; continuar para a página 3 funcionou = {resultados[tec]} ({p.Url.Replace(url, "")})");
            }
        }
        finally { TestEnvironment.Stop(porta); }
        Assert.True(resultados["rp"]);
        Assert.True(resultados["ssr"]);
        Assert.False(resultados["int"]);   // o circuito morreu com o processo: filtro e página se perderam
    }

    [Fact(DisplayName = "04-7 · the old URL /Cobranca/Parcelas.aspx is served by the new screen; the legacy menu still leads to it")]
    public async Task UrlAntigaAtendidaPeloNovo()
    {
        const int porta = 8125;
        var url = await TestEnvironment.StartSpikeAsync("04-ui", "Spike04.Ui.dll", porta, new Dictionary<string, string> { ["Migracao__ParcelasAspx"] = "ssr" });
        try
        {
            var p = await _nav.NewPageAsync();
            await BrowserFixture.LoginAsync(p, url);
            await p.ClickAsync("a:text-is('Parcelas')");   // menu do Site.master do legado
            await p.WaitForLoadStateAsync();
            var tecnologia = await p.GetAttributeAsync("body", "data-tecnologia");
            _saida.WriteLine($"menu do legado -> {p.Url.Replace(url, "")} atendida por: {tecnologia}");
            Assert.Equal("blazor", tecnologia);
            Assert.EndsWith("/Cobranca/Parcelas.aspx", p.Url);

            await p.GotoAsync(url + "/Grupos/MapaCotas.aspx?id=4");   // e uma tela legada que linka para parcelas com parâmetro
            await p.GotoAsync(url + "/Cobranca/Parcelas.aspx?grupo=4");
            Assert.Equal("blazor", await p.GetAttributeAsync("body", "data-tecnologia"));
            Assert.True(await p.Locator("#grade td.nn").CountAsync() > 0);
        }
        finally { TestEnvironment.Stop(porta); }
    }
}
