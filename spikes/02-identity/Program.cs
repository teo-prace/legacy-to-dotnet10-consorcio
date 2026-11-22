// POC-02b · Identidade compartilhada por "ponte de sessão"
//
// O legado só guarda o login em Session("Usuario") (DataRow, InProc). A fachada
// pergunta ao legado quem é o dono do cookie ASP.NET_SessionId (Sessao/Quem.ashx,
// protegido por chave) e transforma a resposta num ClaimsPrincipal do ASP.NET Core.
// Login, logout e expiração continuam sendo do legado (uma única fonte de verdade).
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Transforms;

// ACHADO 1: com um único esquema registrado, o ASP.NET Core (7+) o usa como padrão automaticamente,
// e a autenticação passa a rodar em TODA requisição repassada ao legado. Desligado aqui.
AppContext.SetSwitch("Microsoft.AspNetCore.Authentication.SuppressAutoDefaultScheme", true);

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

builder.Services.AddReverseProxy()
    .LoadFromConfig(cfg.GetSection("ReverseProxy"))
    .AddTransforms(c => c.AddResponseHeader("X-SGC-Origem", "legado", append: false));

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("legado", c =>
    {
        c.BaseAddress = new Uri(cfg["Legado:Url"]!);
        c.Timeout = TimeSpan.FromSeconds(5);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false });

// Nenhum esquema padrão de autenticação: só as rotas novas que exigem usuário consultam a ponte.
// (Se fosse padrão, cada requisição repassada ao legado pagaria uma chamada extra.)
builder.Services.AddAuthentication(o => o.DefaultChallengeScheme = PonteSessao.Esquema)
    .AddScheme<PonteSessaoOpcoes, PonteSessaoHandler>(PonteSessao.Esquema, o =>
    {
        o.Chave = cfg["Legado:PonteChave"]!;
        o.CacheSegundos = cfg.GetValue("Legado:PonteCacheSegundos", 5);
    });
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("Logado", p => p.AddAuthenticationSchemes(PonteSessao.Esquema).RequireAuthenticatedUser());
    o.AddPolicy("Administrador", p => p.AddAuthenticationSchemes(PonteSessao.Esquema).RequireClaim("perfil", "1"));
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/novo/ping", () => "ok");
app.MapGet("/novo/diag/chamadas-ponte", () => PonteSessaoHandler.ChamadasAPonte);   // só na POC

app.MapGet("/novo/quem-sou-eu", (ClaimsPrincipal u, HttpContext c) =>
{
    c.Response.Headers["X-SGC-Origem"] = "novo";
    return Results.Json(new
    {
        codigo = u.FindFirstValue(ClaimTypes.NameIdentifier),
        login = u.FindFirstValue("login"),
        nome = u.FindFirstValue(ClaimTypes.Name),
        perfil = u.FindFirstValue("perfil"),
    });
}).RequireAuthorization("Logado");

app.MapGet("/novo/admin", (ClaimsPrincipal u) => Results.Text("área administrativa: " + u.Identity!.Name))
   .RequireAuthorization("Administrador");

// A ponte é interna: de fora, mesmo pela fachada, não existe.
app.Map("/Sessao/{**resto}", () => Results.NotFound());

app.MapReverseProxy();
app.Run();

public static class PonteSessao { public const string Esquema = "SessaoLegado"; }

public class PonteSessaoOpcoes : AuthenticationSchemeOptions
{
    public string Chave { get; set; } = "";
    public int CacheSegundos { get; set; } = 5;
}

public record UsuarioLegado(int Codigo, string Login, string Nome, int Perfil, int ExpiraEmMinutos);

public class PonteSessaoHandler(
    IOptionsMonitor<PonteSessaoOpcoes> opcoes, ILoggerFactory log, UrlEncoder enc,
    IHttpClientFactory http, IMemoryCache cache)
    : AuthenticationHandler<PonteSessaoOpcoes>(opcoes, log, enc)
{
    public static long ChamadasAPonte;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var sessao = Request.Cookies["ASP.NET_SessionId"];
        if (string.IsNullOrEmpty(sessao)) return AuthenticateResult.NoResult();

        var chaveCache = "ponte:" + sessao;
        if (!cache.TryGetValue(chaveCache, out UsuarioLegado? usuario))
        {
            Interlocked.Increment(ref ChamadasAPonte);
            using var req = new HttpRequestMessage(HttpMethod.Get, "Sessao/Quem.ashx");
            req.Headers.Add("Cookie", "ASP.NET_SessionId=" + sessao);
            req.Headers.Add("X-SGC-Ponte", Options.Chave);
            try
            {
                using var resp = await http.CreateClient("legado").SendAsync(req, Context.RequestAborted);
                usuario = resp.IsSuccessStatusCode ? await resp.Content.ReadFromJsonAsync<UsuarioLegado>() : null;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return AuthenticateResult.Fail("legado indisponível: " + ex.Message);
            }
            // ACHADO 2: só guarda resposta positiva. O cookie de sessão do legado nasce antes do login
            // (Global.asax tem Session_Start); guardar "ninguém logado" fazia a tela nova ignorar o
            // login recém-feito e voltar para o Login em ciclo.
            if (usuario is not null && Options.CacheSegundos > 0)   // 0 = sem cache (IMemoryCache não aceita expiração zero)
                cache.Set(chaveCache, usuario, TimeSpan.FromSeconds(Options.CacheSegundos));
        }

        if (usuario is null) return AuthenticateResult.NoResult();

        var identidade = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Codigo.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim("login", usuario.Login),
            new Claim("perfil", usuario.Perfil.ToString()),
        }, PonteSessao.Esquema);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identidade), PonteSessao.Esquema));
    }

    // sem sessão: manda para o login do legado, que devolve para a tela nova (ReturnUrl)
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Redirect("/Login.aspx?ReturnUrl=" + Uri.EscapeDataString(Request.Path + Request.QueryString));
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        return Response.WriteAsync("Acesso negado para o seu perfil.");
    }
}
