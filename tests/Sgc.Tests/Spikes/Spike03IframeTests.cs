using Microsoft.Playwright;
using Sgc.Tests.Infra;
using Xunit.Abstractions;

namespace Sgc.Tests.Spikes;

/// <summary>
/// POC-03 · iframe. Fachada com proteção anti-framing em 8103, sem proteção em 8113.
/// "Outro site" = a mesma fachada aberta por 127.0.0.1 (origem e site diferentes de localhost).
/// </summary>
[Collection("browser")]
[Trait("Spike", "03")]
public class Spike03IframeTests : IAsyncLifetime
{
    const string Protegida = "http://localhost:8103";
    const string Desprotegida = "http://localhost:8113";
    readonly BrowserFixture _nav;
    readonly ITestOutputHelper _saida;

    public Spike03IframeTests(BrowserFixture nav, ITestOutputHelper saida) { _nav = nav; _saida = saida; }

    public async Task InitializeAsync()
    {
        await TestEnvironment.EnsureLegacyAsync();
        if (!await TestEnvironment.RespondsAsync(Protegida + "/novo/ping"))
            await TestEnvironment.StartSpikeAsync("03-iframe", "Spike03.Iframe.dll", 8103);
        if (!await TestEnvironment.RespondsAsync(Desprotegida + "/novo/ping"))
            await TestEnvironment.StartSpikeAsync("03-iframe", "Spike03.Iframe.dll", 8113, new Dictionary<string, string> { ["Fachada__ProtegerFraming"] = "false" });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    static async Task<bool> FrameMostra(IPage pagina, string idFrame, string seletor)
    {
        try { return await pagina.FrameLocator("#" + idFrame).Locator(seletor).CountAsync() > 0; }
        catch { return false; }
    }

    static async Task EsperarFrameAsync(IPage pagina, string idFrame)
    {
        // espera o iframe terminar de carregar (ou falhar ao carregar, quando bloqueado)
        await pagina.WaitForFunctionAsync($"() => {{ const f = document.getElementById('{idFrame}'); return !!(f && f.src); }}");
        await Task.Delay(1500);
    }

    [Fact(DisplayName = "03-1 · clickjacking: any site can frame the legacy system today; the facade prevents it")]
    public async Task Clickjacking()
    {
        var pagina = await _nav.NewPageAsync();
        await pagina.GotoAsync("http://127.0.0.1:8103/novo/externo.html");
        await EsperarFrameAsync(pagina, "direto");
        await EsperarFrameAsync(pagina, "pelaFachada");
        var diretoCarregou = await FrameMostra(pagina, "direto", "#txtLogin");
        var fachadaCarregou = await FrameMostra(pagina, "pelaFachada", "#txtLogin");
        _saida.WriteLine($"outro site emoldurando o legado direto (sem cabeçalho): {diretoCarregou}");
        _saida.WriteLine($"outro site emoldurando pela fachada (X-Frame-Options + frame-ancestors): {fachadaCarregou}");
        Assert.True(diretoCarregou);     // vulnerabilidade do legado
        Assert.False(fachadaCarregou);   // bloqueado pelo navegador

        var semProtecao = await _nav.NewPageAsync();
        await semProtecao.GotoAsync("http://127.0.0.1:8113/novo/externo.html");
        await EsperarFrameAsync(semProtecao, "pelaFachada");
        Assert.True(await FrameMostra(semProtecao, "pelaFachada", "#txtLogin"));   // controle: sem os cabeçalhos, emoldura
    }

    [Fact(DisplayName = "03-2 · same origin: a new shell hosts the logged-in legacy screen, with postbacks inside the frame")]
    public async Task ShellMesmaOrigem()
    {
        var pagina = await _nav.NewPageAsync();
        await BrowserFixture.LoginAsync(pagina, Protegida);
        await pagina.GotoAsync(Protegida + "/novo/shell.html?tela=" + Uri.EscapeDataString("/Cobranca/Parcelas.aspx?grupo=4"));
        var frame = pagina.FrameLocator("#legado");
        await frame.Locator("#ctl00_cphConteudo_gvParcelas").WaitForAsync();
        await frame.Locator("#ctl00_cphConteudo_ddlSituacao").SelectOptionAsync("3");
        await frame.Locator("#ctl00_cphConteudo_btnPesquisar").ClickAsync();
        await frame.Locator("td:text-is('Paga')").First.WaitForAsync();
        var altura = await pagina.EvalOnSelectorAsync<int>("#legado", "f => f.getBoundingClientRect().height");
        _saida.WriteLine($"postback dentro do frame ok; altura ajustada do iframe: {altura}px; URL do shell: {pagina.Url}");
        Assert.True(altura > 600);
        Assert.Contains("#/Cobranca/Parcelas.aspx", pagina.Url);
    }

    [Fact(DisplayName = "03-3 · deep link and refresh: without sync the inner screen is lost; with sync it comes back")]
    public async Task LinkDiretoEF5()
    {
        foreach (var sync in new[] { "0", "1" })
        {
            var pagina = await _nav.NewPageAsync();
            await BrowserFixture.LoginAsync(pagina, Protegida);
            await pagina.GotoAsync($"{Protegida}/novo/shell.html?sync={sync}&tela=" + Uri.EscapeDataString("/Grupos/Grupos.aspx"));
            var frame = pagina.FrameLocator("#legado");
            await frame.Locator("a:text('002071')").ClickAsync();   // navega dentro do frame
            await frame.Locator("text=Constituição").Or(frame.Locator("h1:has-text('002071')")).First.WaitForAsync();
            await Task.Delay(500);
            await pagina.ReloadAsync();
            await Task.Delay(1500);
            var urlInterna = pagina.Frames.First(f => f.ParentFrame == pagina.MainFrame).Url;
            _saida.WriteLine($"sync={sync}: depois do F5 o frame mostra {urlInterna}");
            if (sync == "0") Assert.Contains("Grupos.aspx", urlInterna);        // perdeu a tela onde estava
            else Assert.Contains("GrupoEdicao.aspx", urlInterna);              // voltou para onde estava
        }
    }

    [Fact(DisplayName = "03-4 · back button: navigations inside the frame go into the window history")]
    public async Task BotaoVoltar()
    {
        var pagina = await _nav.NewPageAsync();
        await BrowserFixture.LoginAsync(pagina, Protegida);
        await pagina.GotoAsync($"{Protegida}/novo/shell.html?sync=0&tela=" + Uri.EscapeDataString("/Grupos/Grupos.aspx"));
        var frame = pagina.FrameLocator("#legado");
        await frame.Locator("a:text('002071')").ClickAsync();
        await Task.Delay(1000);
        var urlShellAntes = pagina.Url;
        await pagina.EvaluateAsync("() => history.back()");   // o "Voltar" do navegador (só o frame navega)
        await Task.Delay(1500);
        var interno = pagina.Frames.First(f => f.ParentFrame == pagina.MainFrame).Url;
        _saida.WriteLine($"Voltar: URL do shell {(pagina.Url == urlShellAntes ? "não mudou" : "mudou")}, frame voltou para {interno}");
        Assert.Equal(urlShellAntes, pagina.Url);
        Assert.Contains("Grupos.aspx", interno);
    }

    [Fact(DisplayName = "03-5 · boleto popup: opens from a plain iframe, blocked from a sandboxed one")]
    public async Task Popups()
    {
        var pagina = await _nav.NewPageAsync();
        await BrowserFixture.LoginAsync(pagina, Protegida);
        await pagina.GotoAsync(Protegida + "/novo/shell.html?tela=" + Uri.EscapeDataString("/Cobranca/Parcelas.aspx?grupo=4"));
        var popup = await pagina.RunAndWaitForPopupAsync(() => pagina.FrameLocator("#legado").Locator("a:text('Boleto')").First.ClickAsync());
        await popup.WaitForLoadStateAsync();
        _saida.WriteLine("iframe comum: popup abriu em " + popup.Url);
        Assert.Contains("Boleto.aspx", popup.Url);

        var sandbox = await _nav.NewPageAsync();
        await BrowserFixture.LoginAsync(sandbox, Protegida);
        var mensagens = new List<string>();
        sandbox.Console += (_, m) => mensagens.Add(m.Text);
        await sandbox.GotoAsync(Protegida + "/novo/sandbox.html");
        var abriu = true;
        try
        {
            await sandbox.RunAndWaitForPopupAsync(() => sandbox.FrameLocator("#legado").Locator("a:text('Boleto')").First.ClickAsync(),
                new() { Timeout = 4000 });
        }
        catch (TimeoutException) { abriu = false; }
        _saida.WriteLine($"iframe com sandbox sem allow-popups: popup abriu = {abriu}; console: {string.Join(" | ", mensagens.Where(m => m.Contains("Blocked") || m.Contains("sandbox")))}");
        Assert.False(abriu);
    }

    [Fact(DisplayName = "03-6 · confirm() inside the iframe is shown and respected")]
    public async Task ConfirmDentroDoFrame()
    {
        var pagina = await _nav.NewPageAsync();
        await BrowserFixture.LoginAsync(pagina, Protegida);
        await pagina.GotoAsync(Protegida + "/novo/shell.html?tela=" + Uri.EscapeDataString("/Cobranca/Emissao.aspx"));
        string? mensagem = null;
        pagina.Dialog += async (_, d) => { mensagem = d.Message; await d.DismissAsync(); };   // "Cancelar": não emite
        await pagina.FrameLocator("#legado").Locator("#ctl00_cphConteudo_btnEmitir").ClickAsync();
        await Task.Delay(1000);
        var emitiu = await pagina.FrameLocator("#legado").Locator("text=Emissão concluída").CountAsync() > 0;
        _saida.WriteLine($"confirm exibido: \"{mensagem}\"; cancelado -> emitiu = {emitiu}");
        Assert.NotNull(mensagem);
        Assert.False(emitiu);
    }

    [Fact(DisplayName = "03-7 · expired session: unhandled, the login opens INSIDE the frame; handled, in the window and back")]
    public async Task SessaoExpiradaNoFrame()
    {
        foreach (var tratar in new[] { "0", "1" })
        {
            var pagina = await _nav.NewPageAsync();
            await BrowserFixture.LoginAsync(pagina, Protegida);
            await pagina.GotoAsync($"{Protegida}/novo/shell.html?login={tratar}&tela=" + Uri.EscapeDataString("/Grupos/Grupos.aspx"));
            await pagina.FrameLocator("#legado").Locator("a:text('002071')").WaitForAsync();

            // a sessão acaba (Sair em outra aba do mesmo navegador)
            var outra = await pagina.Context.NewPageAsync();
            await outra.GotoAsync(Protegida + "/Default.aspx");
            await Task.WhenAll(outra.WaitForURLAsync("**/Login.aspx"), outra.ClickAsync("#ctl00_lnkSair"));

            await pagina.FrameLocator("#legado").Locator("a:text('002071')").ClickAsync();
            await Task.Delay(2000);
            var loginNoFrame = pagina.Url.Contains("/novo/shell.html") && await FrameMostra(pagina, "legado", "#txtLogin");
            var loginNaJanela = pagina.Url.Contains("/Login.aspx");
            _saida.WriteLine($"login={tratar}: login dentro do frame = {loginNoFrame}, login na janela inteira = {loginNaJanela} ({pagina.Url})");
            if (tratar == "0") Assert.True(loginNoFrame);
            else
            {
                Assert.True(loginNaJanela);
                await pagina.FillAsync("#txtLogin", "msouza");
                await pagina.FillAsync("#txtSenha", "cecons123");
                await pagina.ClickAsync("#btnEntrar");
                await pagina.WaitForURLAsync("**/novo/shell.html**");
                await Task.Delay(1500);
                var interno = pagina.Frames.First(f => f.ParentFrame == pagina.MainFrame).Url;
                _saida.WriteLine($"   depois do login voltou para o shell com a tela {interno}");
                Assert.Contains("GrupoEdicao.aspx?id=4", interno);   // voltou para a tela que tentava abrir
            }
        }
    }

    [Fact(DisplayName = "03-8 · postMessage: the new screen in the frame talks to the shell; a message from another origin is ignored")]
    public async Task PostMessage()
    {
        var pagina = await _nav.NewPageAsync();
        await BrowserFixture.LoginAsync(pagina, Protegida);
        await pagina.GotoAsync(Protegida + "/novo/shell.html?altura=0&tela=" + Uri.EscapeDataString("/novo/filho.html"));
        var filho = pagina.FrameLocator("#legado");
        await filho.Locator("#pedirAltura").ClickAsync();
        await filho.Locator("#respostas:has-text('ok')").WaitForAsync();
        var altura = await pagina.EvalOnSelectorAsync<int>("#legado", "f => f.getBoundingClientRect().height");
        await filho.Locator("#navegar").ClickAsync();
        await Task.Delay(1500);
        var interno = pagina.Frames.First(f => f.ParentFrame == pagina.MainFrame).Url;
        _saida.WriteLine($"filho pediu altura 900 -> {altura}px; filho pediu navegação -> frame em {interno}");
        Assert.Equal(900, altura);
        Assert.Contains("Grupos.aspx", interno);

        // outro site abre o shell e manda mensagem forjada
        var externo = await _nav.NewPageAsync();
        await BrowserFixture.LoginAsync(externo, Protegida);
        var shellAberto = await externo.RunAndWaitForPopupAsync(() => externo.GotoAsync("http://127.0.0.1:8103/novo/externo.html?forjar=1"));
        await Task.Delay(5000);
        var log = await shellAberto.EvaluateAsync<string>("() => window.sgcLog()");
        _saida.WriteLine("log do shell aberto pelo outro site: " + log.Replace("\n", " | "));
        Assert.Contains("IGNORADA mensagem de http://127.0.0.1:8103", log);
        Assert.DoesNotContain("exemplo.invalido", shellAberto.Frames.First(f => f.ParentFrame == shellAberto.MainFrame).Url);
    }

    [Fact(DisplayName = "03-9 · cookies in an iframe from another site: the session (SameSite=Lax) is not sent")]
    public async Task CookiesTerceiros()
    {
        var pagina = await _nav.NewPageAsync();
        await BrowserFixture.LoginAsync(pagina, Desprotegida);   // logado em localhost:8113
        await pagina.GotoAsync("http://127.0.0.1:8113/novo/externo.html?tela=/Default.aspx");   // outro site emoldura a fachada desprotegida
        await EsperarFrameAsync(pagina, "pelaFachada");
        var mostraPainel = await FrameMostra(pagina, "pelaFachada", "text=Painel");
        var mostraLogin = await FrameMostra(pagina, "pelaFachada", "#txtLogin");
        _saida.WriteLine($"iframe de outro site para tela logada: painel = {mostraPainel}, login = {mostraLogin}");
        Assert.False(mostraPainel);
        Assert.True(mostraLogin);
    }
}
