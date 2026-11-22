using System.Diagnostics;
using Microsoft.Playwright;
using Sgc.Tests.Infra;
using Xunit.Abstractions;

namespace Sgc.Tests.Spikes;

/// <summary>
/// POC-02a · System.Web Adapters oficiais (remote session) numa cópia do legado (porta 8094),
/// e a comparação de custo com a ponte de sessão (02b).
/// </summary>
[Collection("browser")]
[Trait("Spike", "02a")]
public class Spike02aAdaptersTests : IAsyncLifetime
{
    readonly BrowserFixture _nav;
    readonly ITestOutputHelper _saida;

    public Spike02aAdaptersTests(BrowserFixture nav, ITestOutputHelper saida) { _nav = nav; _saida = saida; }

    public async Task InitializeAsync()
    {
        await TestEnvironment.EnsureLegacyAsync();
        if (!await TestEnvironment.RespondsAsync("http://localhost:8094/Login.aspx"))
        {
            var script = Path.Combine(TestEnvironment.Root, "spikes", "02a-system-web-adapters", "preparar-legado.ps1");
            using (var prep = Process.Start(new ProcessStartInfo("powershell", $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\"") { UseShellExecute = false, CreateNoWindow = true })!)
                await prep.WaitForExitAsync();
            await TestEnvironment.StartIisExpressAsync(Path.Combine(Path.GetTempPath(), "sgc-web-adaptadores"), 8094);
        }
        if (!await TestEnvironment.RespondsAsync("http://localhost:8104/novo/ping"))
            await TestEnvironment.StartSpikeAsync("02a-system-web-adapters", "Spike02a.Adapters.dll", 8104);
        if (!await TestEnvironment.RespondsAsync("http://localhost:8102/novo/ping"))
            await TestEnvironment.StartSpikeAsync("02-identity", "Spike02.Identity.dll", 8102);
        if (!await TestEnvironment.RespondsAsync("http://localhost:8122/novo/ping"))
            await TestEnvironment.StartSpikeAsync("02-identity", "Spike02.Identity.dll", 8122, new Dictionary<string, string> { ["Legado__PonteCacheSegundos"] = "0" });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "02a-1 · System.Web adapters: the new screen reads the user from the legacy session")]
    public async Task AdaptadoresLeemSessao()
    {
        var pagina = await _nav.NewPageAsync();
        pagina.SetDefaultTimeout(120_000);   // primeira visita compila a cópia
        await BrowserFixture.LoginAsync(pagina, "http://localhost:8104", "msouza");
        await pagina.GotoAsync("http://localhost:8104/novo/quem-sou-eu");
        var corpo = await pagina.InnerTextAsync("body");
        _saida.WriteLine(corpo);
        Assert.Contains("\"nome\":\"Marcia Souza\"", corpo);
        await pagina.GotoAsync("http://localhost:8104/Grupos/Grupos.aspx");   // o legado continua normal
        Assert.DoesNotContain("Login.aspx", pagina.Url);
    }

    [Fact(DisplayName = "02a-2 · identity cost per request: adapters vs bridge (with and without cache)")]
    public async Task CustoDaIdentidade()
    {
        var resultados = new List<string>();
        foreach (var (url, rotulo) in new[]
                 {
                     ("http://localhost:8104", "02a adaptadores oficiais (sem cache)"),
                     ("http://localhost:8102", "02b ponte, cache 5 s"),
                     ("http://localhost:8122", "02b ponte, sem cache"),
                 })
        {
            var pagina = await _nav.NewPageAsync();
            pagina.SetDefaultTimeout(120_000);
            await BrowserFixture.LoginAsync(pagina, url, "msouza");
            // cabeçalho Cookie montado à mão: o CookieContainer, com Domain explícito, manda no formato
            // RFC 2965 ($Version=1; ...; $Domain=...) e estraga o ID de sessão
            var cookie = string.Join("; ", (await pagina.Context.CookiesAsync(new[] { url })).Select(c => $"{c.Name}={c.Value}"));
            using var http = new HttpClient(new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false });
            http.DefaultRequestHeaders.Add("Cookie", cookie);
            for (var i = 0; i < 5; i++) await http.GetAsync(url + "/novo/quem-sou-eu");
            var tempos = new List<double>();
            for (var i = 0; i < 40; i++)
            {
                var sw = Stopwatch.StartNew();
                using var r = await http.GetAsync(url + "/novo/quem-sou-eu");
                var corpo = await r.Content.ReadAsStringAsync();
                Assert.True(r.IsSuccessStatusCode, $"{rotulo}: {(int)r.StatusCode} cookie=[{cookie}] {corpo[..Math.Min(300, corpo.Length)]}");
                tempos.Add(sw.Elapsed.TotalMilliseconds);
            }
            tempos.Sort();
            resultados.Add($"{rotulo,-40} mediana {tempos[20],6:N1} ms   p90 {tempos[36],6:N1} ms");
        }
        foreach (var r in resultados) _saida.WriteLine(r);
    }
}
