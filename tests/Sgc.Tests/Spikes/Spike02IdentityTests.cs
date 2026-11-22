using System.Diagnostics;
using System.Net;
using Microsoft.Playwright;
using Sgc.Tests.Infra;
using Xunit.Abstractions;

namespace Sgc.Tests.Spikes;

/// <summary>
/// POC-02b · Identidade compartilhada pela ponte de sessão (Sessao/Quem.ashx).
/// Fachada em http://localhost:8102.
/// </summary>
[Collection("browser")]
[Trait("Spike", "02")]
public class Spike02IdentityTests : IAsyncLifetime
{
    const string Fachada = "http://localhost:8102";
    const string Chave = "dev-ponte-7f3a9c2e-troque-em-producao";
    readonly BrowserFixture _nav;
    readonly ITestOutputHelper _saida;

    public Spike02IdentityTests(BrowserFixture nav, ITestOutputHelper saida) { _nav = nav; _saida = saida; }

    public async Task InitializeAsync()
    {
        await TestEnvironment.EnsureLegacyAsync();
        if (!await TestEnvironment.RespondsAsync(Fachada + "/novo/ping"))
            await TestEnvironment.StartSpikeAsync("02-identity", "Spike02.Identity.dll", 8102);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "02-1 · new screen without a session goes to the legacy login and comes back logged in (ReturnUrl)")]
    public async Task LoginDoLegadoDevolveParaTelaNova()
    {
        var pagina = await _nav.NewPageAsync();
        await pagina.GotoAsync(Fachada + "/novo/quem-sou-eu");
        Assert.Contains("/Login.aspx?ReturnUrl=%2Fnovo%2Fquem-sou-eu", pagina.Url, StringComparison.OrdinalIgnoreCase);

        await pagina.FillAsync("#txtLogin", "msouza");
        await pagina.FillAsync("#txtSenha", "cecons123");
        await Task.WhenAll(pagina.WaitForURLAsync("**/novo/quem-sou-eu"), pagina.ClickAsync("#btnEntrar"));
        var corpo = await pagina.InnerTextAsync("body");
        _saida.WriteLine(corpo);
        Assert.Contains("\"login\":\"msouza\"", corpo);
        Assert.Contains("Marcia Souza", corpo);
        Assert.Contains("\"perfil\":\"2\"", corpo);
    }

    [Fact(DisplayName = "02-2 · legacy profile becomes authorization in the new app (operator 403, admin 200)")]
    public async Task PerfilViraAutorizacao()
    {
        var operador = await _nav.NewPageAsync();
        await BrowserFixture.LoginAsync(operador, Fachada, "msouza");
        var r1 = await operador.GotoAsync(Fachada + "/novo/admin");
        Assert.Equal(403, r1!.Status);

        var admin = await _nav.NewPageAsync();
        await BrowserFixture.LoginAsync(admin, Fachada, "admin");
        var r2 = await admin.GotoAsync(Fachada + "/novo/admin");
        Assert.Equal(200, r2!.Status);
        Assert.Contains("Administrador do sistema", await admin.InnerTextAsync("body"));
    }

    [Fact(DisplayName = "02-3 · logout in the legacy system ends the session on the new screen (within the cache time)")]
    public async Task LogoutNoLegadoValeNoNovo()
    {
        var pagina = await _nav.NewPageAsync();
        await BrowserFixture.LoginAsync(pagina, Fachada, "msouza");
        Assert.Equal(200, (await pagina.GotoAsync(Fachada + "/novo/quem-sou-eu"))!.Status);

        await pagina.GotoAsync(Fachada + "/Default.aspx");
        await Task.WhenAll(pagina.WaitForURLAsync("**/Login.aspx"), pagina.ClickAsync("#ctl00_lnkSair"));
        var sw = Stopwatch.StartNew();

        // Session.Abandon não troca o cookie: o mesmo ASP.NET_SessionId agora não tem usuário.
        string url;
        do
        {
            await pagina.GotoAsync(Fachada + "/novo/quem-sou-eu");
            url = pagina.Url;
            if (url.Contains("Login.aspx")) break;
            await Task.Delay(500);
        } while (sw.Elapsed < TimeSpan.FromSeconds(15));
        _saida.WriteLine($"tela nova pediu login {sw.ElapsedMilliseconds} ms depois do Sair (cache da ponte: 5 s)");
        Assert.Contains("Login.aspx", url);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(7));
    }

    [Fact(DisplayName = "02-4 · the bridge is not reachable from outside: 404 through the facade, even with the key")]
    public async Task PonteFechadaParaFora()
    {
        using var http = TestEnvironment.Client();
        var req = new HttpRequestMessage(HttpMethod.Get, Fachada + "/Sessao/Quem.ashx");
        req.Headers.Add("X-SGC-Ponte", Chave);
        Assert.Equal(HttpStatusCode.NotFound, (await http.SendAsync(req)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync(TestEnvironment.LegacyUrl + "/Sessao/Quem.ashx")).StatusCode);   // direto, sem chave
    }

    [Fact(DisplayName = "02-5 · forwarded legacy screens don't call the bridge")]
    public async Task LegadoNaoPagaAPonte()
    {
        using var http = TestEnvironment.Client();
        var antes = long.Parse(await http.GetStringAsync(Fachada + "/novo/diag/chamadas-ponte"));
        var pagina = await _nav.NewPageAsync();
        await BrowserFixture.LoginAsync(pagina, Fachada, "msouza");
        foreach (var t in new[] { "Default.aspx", "Grupos/Grupos.aspx", "Cobranca/Parcelas.aspx", "Assembleia/Assembleias.aspx", "Comercial/Propostas.aspx" })
            await pagina.GotoAsync($"{Fachada}/{t}");
        var depois = long.Parse(await http.GetStringAsync(Fachada + "/novo/diag/chamadas-ponte"));
        _saida.WriteLine($"chamadas à ponte durante login + 5 telas legadas: {depois - antes}");
        Assert.Equal(antes, depois);
    }

    [Fact(DisplayName = "02-6 · using only new screens keeps the legacy session alive (1 min timeout)")]
    public async Task PonteRenovaASessaoDoLegado()
    {
        // cópia do legado com timeout de sessão = 1 minuto, na porta 8093, e uma fachada apontando para ela
        var copia = Path.Combine(Path.GetTempPath(), "sgc-web-timeout1");
        if (Directory.Exists(copia)) Directory.Delete(copia, true);
        Copiar(Path.Combine(TestEnvironment.Root, "legacy", "src", "SGC.Web"), copia);
        var wc = Path.Combine(copia, "web.config");
        File.WriteAllText(wc, File.ReadAllText(wc).Replace("timeout=\"60\"", "timeout=\"1\""));

        await TestEnvironment.StartIisExpressAsync(copia, 8093);
        var fachada = await TestEnvironment.StartSpikeAsync("02-identity", "Spike02.Identity.dll", 8112, new Dictionary<string, string>
        {
            ["Legado__Url"] = "http://localhost:8093/",
            ["ReverseProxy__Clusters__legado__Destinations__iisexpress__Address"] = "http://localhost:8093/",
        });
        try
        {
            // aquecimento: a cópia compila as páginas na primeira visita (Web Site project)
            var aquece = await _nav.NewPageAsync();
            aquece.SetDefaultTimeout(120_000);
            await BrowserFixture.LoginAsync(aquece, fachada, "msouza");
            await aquece.GotoAsync(fachada + "/Grupos/Grupos.aspx");

            // A: só tela nova, a cada 20 s, por 80 s -> legado continua logado
            var a = await _nav.NewPageAsync();
            await BrowserFixture.LoginAsync(a, fachada, "msouza");
            for (var t = 0; t < 4; t++) { await Task.Delay(20_000); Assert.Equal(200, (await a.GotoAsync(fachada + "/novo/quem-sou-eu"))!.Status); }
            await a.GotoAsync(fachada + "/Grupos/Grupos.aspx");
            var aLogado = !a.Url.Contains("Login.aspx");

            // B (controle): nenhuma atividade por 80 s -> legado expira
            var b = await _nav.NewPageAsync();
            await BrowserFixture.LoginAsync(b, fachada, "msouza");
            await Task.Delay(80_000);
            await b.GotoAsync(fachada + "/Grupos/Grupos.aspx");
            var bLogado = !b.Url.Contains("Login.aspx");

            _saida.WriteLine($"A (só telas novas por 80 s): legado logado = {aLogado}; B (parado 80 s): legado logado = {bLogado}");
            Assert.True(aLogado);
            Assert.False(bLogado);
        }
        finally
        {
            TestEnvironment.Stop(8112);
            TestEnvironment.Stop(8093);
        }
    }

    static void Copiar(string de, string para)
    {
        foreach (var d in Directory.GetDirectories(de, "*", SearchOption.AllDirectories)) Directory.CreateDirectory(d.Replace(de, para));
        Directory.CreateDirectory(para);
        foreach (var f in Directory.GetFiles(de, "*", SearchOption.AllDirectories)) File.Copy(f, f.Replace(de, para), true);
    }
}
