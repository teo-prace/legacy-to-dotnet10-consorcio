using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Sgc.Tests.Infra;
using Xunit.Abstractions;

namespace Sgc.Tests.Spikes;

/// <summary>
/// POC-01 · Fachada YARP (.NET 10) na frente do WebForms, sem mudar nada no legado.
/// Fachada em http://localhost:8100 -> legado http://localhost:8090.
/// </summary>
[Collection("browser")]
[Trait("Spike", "01")]
public class Spike01FacadeTests : IAsyncLifetime
{
    const int Porta = 8100;
    const string Fachada = "http://localhost:8100";
    readonly BrowserFixture _nav;
    readonly ITestOutputHelper _saida;

    public Spike01FacadeTests(BrowserFixture nav, ITestOutputHelper saida) { _nav = nav; _saida = saida; }

    public async Task InitializeAsync()
    {
        await TestEnvironment.EnsureLegacyAsync();
        if (!await TestEnvironment.RespondsAsync(Fachada + "/novo/ping"))
            await TestEnvironment.StartSpikeAsync("01-facade", "Spike01.Facade.dll", Porta);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    static readonly string[] Telas =
    {
        "Default.aspx", "Cadastro/Consorciados.aspx", "Cadastro/ConsorciadoEdicao.aspx?id=1", "Cadastro/Bens.aspx",
        "Cadastro/BemEdicao.aspx?id=8", "Cadastro/Indices.aspx", "Cadastro/PontosVenda.aspx", "Comercial/Planos.aspx",
        "Comercial/PlanoEdicao.aspx?id=3", "Comercial/Simulacao.aspx", "Comercial/Propostas.aspx", "Comercial/PropostaEdicao.aspx",
        "Comercial/PropostaEdicao.aspx?id=1", "Grupos/Grupos.aspx", "Grupos/GrupoEdicao.aspx?id=4", "Grupos/MapaCotas.aspx?id=4",
        "Grupos/Extrato.aspx?id=4", "Cotas/CotaDetalhe.aspx?id=241", "Assembleia/Assembleias.aspx?grupo=4",
        "Assembleia/Contemplacoes.aspx", "Cobranca/Parcelas.aspx?grupo=4", "Cobranca/Baixa.aspx", "Cobranca/Emissao.aspx",
        "Cobranca/Inadimplencia.aspx", "Cobranca/Retornos.aspx", "Admin/Usuarios.aspx",
    };

    [Fact(DisplayName = "01a · every legacy screen opens through the facade, logged in, served by the legacy system")]
    public async Task TodasAsTelasPelaFachada()
    {
        var pagina = await _nav.NewPageAsync();
        await BrowserFixture.LoginAsync(pagina, Fachada, "admin");
        Assert.StartsWith(Fachada, pagina.Url);   // o redirect pós-login não vazou a porta do legado

        var falhas = new List<string>();
        foreach (var t in Telas)
        {
            var r = await pagina.GotoAsync($"{Fachada}/{t}");
            var html = await pagina.ContentAsync();
            if (r!.Status != 200 || html.Contains("Erro de Servidor") || html.Contains("Server Error") || pagina.Url.Contains("Login.aspx"))
                falhas.Add($"{t}: {r.Status} {pagina.Url}");
            else if (r.Headers.GetValueOrDefault("x-sgc-origem") != "legado")
                falhas.Add($"{t}: origem {r.Headers.GetValueOrDefault("x-sgc-origem")}");
        }
        _saida.WriteLine($"{Telas.Length} telas, {falhas.Count} falhas");
        Assert.Empty(falhas);
    }

    [Fact(DisplayName = "01b · postback with ViewState and AutoPostBack (quota simulation) works through the facade")]
    public async Task PostbackPelaFachada()
    {
        var pagina = await _nav.NewPageAsync();
        await BrowserFixture.LoginAsync(pagina, Fachada);
        await pagina.GotoAsync(Fachada + "/Comercial/Simulacao.aspx");
        await Task.WhenAll(pagina.WaitForLoadStateAsync(LoadState.NetworkIdle),
                           pagina.SelectOptionAsync("#ctl00_cphConteudo_ddlPlano", "3"));   // AutoPostBack
        await pagina.WaitForFunctionAsync("() => document.querySelectorAll('#ctl00_cphConteudo_ddlBem option').length > 2");
        await pagina.SelectOptionAsync("#ctl00_cphConteudo_ddlBem", "8");
        await Task.WhenAll(pagina.WaitForLoadStateAsync(LoadState.NetworkIdle), pagina.ClickAsync("#ctl00_cphConteudo_btnSimular"));
        var html = await pagina.ContentAsync();
        Assert.Contains("R$ 2.346,67", html);
        Assert.Contains("R$ 1.546,67", html);
        Assert.StartsWith(Fachada, pagina.Url);
    }

    [Fact(DisplayName = "01c · legacy redirects (login, expired session, logout) stay on the facade")]
    public async Task RedirectsNaoVazamPortaDoLegado()
    {
        using var http = TestEnvironment.Client(followRedirects: false);
        var r = await http.GetAsync(Fachada + "/Grupos/Grupos.aspx");   // sem sessão -> PaginaBase redireciona
        Assert.Equal(HttpStatusCode.Found, r.StatusCode);
        var location = r.Headers.Location!.ToString();
        _saida.WriteLine("Location: " + location);
        Assert.DoesNotContain("8090", location);
        Assert.StartsWith("/Login.aspx", location);

        var pagina = await _nav.NewPageAsync();
        await BrowserFixture.LoginAsync(pagina, Fachada);
        await Task.WhenAll(pagina.WaitForURLAsync("**/Login.aspx"), pagina.ClickAsync("#ctl00_lnkSair"));   // LinkButton (__doPostBack) + Session.Abandon
        Assert.StartsWith(Fachada + "/Login.aspx", pagina.Url);
    }

    [Fact(DisplayName = "01d · boleto in a new window (target=_blank) opens through the facade")]
    public async Task BoletoEmNovaJanela()
    {
        var pagina = await _nav.NewPageAsync();
        await BrowserFixture.LoginAsync(pagina, Fachada);
        await pagina.GotoAsync(Fachada + "/Cobranca/Parcelas.aspx?grupo=4");
        var popup = await pagina.RunAndWaitForPopupAsync(() => pagina.Locator("a:text('Boleto')").First.ClickAsync());
        await popup.WaitForLoadStateAsync();
        Assert.StartsWith(Fachada + "/Cobranca/Boleto.aspx", popup.Url);
        Assert.Matches(@"\d{5}\.\d{5} \d{5}\.\d{6} \d{5}\.\d{6} \d \d{14}", await popup.ContentAsync());
    }

    [Fact(DisplayName = "01e · the legacy session cookie is issued for the facade's host")]
    public async Task CookieDeSessao()
    {
        var pagina = await _nav.NewPageAsync();
        await BrowserFixture.LoginAsync(pagina, Fachada);
        var cookies = await pagina.Context.CookiesAsync(new[] { Fachada });
        var sessao = cookies.Single(c => c.Name == "ASP.NET_SessionId");
        _saida.WriteLine($"cookie {sessao.Name} domain={sessao.Domain} path={sessao.Path} httpOnly={sessao.HttpOnly} sameSite={sessao.SameSite} secure={sessao.Secure}");
        Assert.Equal("localhost", sessao.Domain);
        Assert.Equal("/", sessao.Path);
    }

    [Fact(DisplayName = "01f · route switch: turns the new screen on and rolls back to the legacy one without a restart")]
    public async Task RollbackSemReiniciar()
    {
        // instância separada, com appsettings próprio numa cópia da pasta bin (não mexe no repositório)
        var origem = Path.Combine(TestEnvironment.Root, "spikes", "01-facade", "bin", "Debug", "net10.0");
        var copia = Path.Combine(Path.GetTempPath(), "poc01-rollback");
        if (Directory.Exists(copia)) Directory.Delete(copia, true);
        CopiarPasta(origem, copia);
        var cfg = Path.Combine(copia, "appsettings.json");

        var psi = new ProcessStartInfo("dotnet", $"\"{Path.Combine(copia, "Spike01.Facade.dll")}\"")
        { WorkingDirectory = copia, UseShellExecute = false, CreateNoWindow = true };
        psi.Environment["Urls"] = "http://localhost:8109";
        using var proc = Process.Start(psi)!;
        try
        {
            using var http = TestEnvironment.Client();
            for (var i = 0; i < 60 && !await TestEnvironment.RespondsAsync("http://localhost:8109/novo/ping"); i++) await Task.Delay(500);

            async Task<string?> Origem() => (await http.GetAsync("http://localhost:8109/Cobranca/Parcelas.aspx")).Headers.TryGetValues("X-SGC-Origem", out var v) ? v.First() : null;

            Assert.Equal("legado", await Origem());
            File.WriteAllText(cfg, File.ReadAllText(cfg).Replace("\"ParcelasNoNovo\": false", "\"ParcelasNoNovo\": true"));
            var sw = Stopwatch.StartNew();
            while (await Origem() != "novo" && sw.Elapsed < TimeSpan.FromSeconds(10)) await Task.Delay(200);
            _saida.WriteLine($"ligou em {sw.ElapsedMilliseconds} ms");
            Assert.Equal("novo", await Origem());

            File.WriteAllText(cfg, File.ReadAllText(cfg).Replace("\"ParcelasNoNovo\": true", "\"ParcelasNoNovo\": false"));
            sw.Restart();
            while (await Origem() != "legado" && sw.Elapsed < TimeSpan.FromSeconds(10)) await Task.Delay(200);
            _saida.WriteLine($"rollback em {sw.ElapsedMilliseconds} ms, mesmo processo (pid {proc.Id}, vivo: {!proc.HasExited})");
            Assert.Equal("legado", await Origem());
        }
        finally { try { proc.Kill(true); } catch { } }
    }

    [Fact(DisplayName = "01g · legacy system down: the facade shows a friendly 503 page, not a raw error")]
    public async Task LegadoForaDoAr()
    {
        var url = await TestEnvironment.StartSpikeAsync("01-facade", "Spike01.Facade.dll", 8108,
            new Dictionary<string, string> { ["ReverseProxy__Clusters__legado__Destinations__iisexpress__Address"] = "http://localhost:8099/" });
        try
        {
            using var http = TestEnvironment.Client();
            var r = await http.GetAsync(url + "/Login.aspx");
            var corpo = await r.Content.ReadAsStringAsync();
            _saida.WriteLine($"{(int)r.StatusCode}: {Regex.Replace(corpo, "<[^>]+>", " ").Trim()}");
            Assert.Equal(HttpStatusCode.ServiceUnavailable, r.StatusCode);
            Assert.Contains("temporariamente indisponível", corpo);
            Assert.Equal("ok", (await http.GetStringAsync(url + "/novo/ping")).Contains(".NET") ? "ok" : "falhou");   // as rotas novas continuam no ar
        }
        finally { TestEnvironment.Stop(8108); }
    }

    [Fact(DisplayName = "01h · cost of the facade: latency direct vs through the proxy")]
    public async Task CustoDaFachada()
    {
        using var http = TestEnvironment.Client();
        async Task<double> Mediana(string url)
        {
            for (var i = 0; i < 5; i++) await http.GetAsync(url);   // aquecimento
            var tempos = new List<double>();
            for (var i = 0; i < 60; i++)
            {
                var sw = Stopwatch.StartNew();
                using var r = await http.GetAsync(url);
                await r.Content.ReadAsByteArrayAsync();
                tempos.Add(sw.Elapsed.TotalMilliseconds);
            }
            tempos.Sort();
            return tempos[tempos.Count / 2];
        }
        var direto = await Mediana(TestEnvironment.LegacyUrl + "/Login.aspx");
        var proxy = await Mediana(Fachada + "/Login.aspx");
        _saida.WriteLine($"Login.aspx mediana: direto {direto:N2} ms, via fachada {proxy:N2} ms, custo {proxy - direto:N2} ms");
        Assert.True(proxy - direto < 20, "fachada adicionou mais de 20 ms");
    }

    static void CopiarPasta(string de, string para)
    {
        foreach (var dir in Directory.GetDirectories(de, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dir.Replace(de, para));
        Directory.CreateDirectory(para);
        foreach (var f in Directory.GetFiles(de, "*", SearchOption.AllDirectories))
            File.Copy(f, f.Replace(de, para), true);
    }
}
