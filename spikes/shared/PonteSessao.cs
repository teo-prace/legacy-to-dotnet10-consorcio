// Ponte de sessão com o legado (decidida na POC-02). Compartilhada pelas POCs seguintes.
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Pocs.Compartilhado;

public static class PonteSessao
{
    public const string Esquema = "SessaoLegado";

    public static IServiceCollection AddPonteSessao(this IServiceCollection s, IConfiguration cfg)
    {
        // sem esquema padrão automático (POC-02, achado 1)
        AppContext.SetSwitch("Microsoft.AspNetCore.Authentication.SuppressAutoDefaultScheme", true);
        s.AddMemoryCache();
        s.AddHttpClient("legado", c => { c.BaseAddress = new Uri(cfg["Legado:Url"]!); c.Timeout = TimeSpan.FromSeconds(5); })
         .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false });
        s.AddAuthentication(o => o.DefaultChallengeScheme = Esquema)
         .AddScheme<PonteSessaoOpcoes, PonteSessaoHandler>(Esquema, o =>
         {
             o.Chave = cfg["Legado:PonteChave"]!;
             o.CacheSegundos = cfg.GetValue("Legado:PonteCacheSegundos", 5);
         });
        s.AddAuthorization(o =>
        {
            o.AddPolicy("Logado", p => p.AddAuthenticationSchemes(Esquema).RequireAuthenticatedUser());
            o.AddPolicy("Operador", p => p.AddAuthenticationSchemes(Esquema).RequireClaim("perfil", "1", "2"));
        });
        return s;
    }
}

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
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var sessao = Request.Cookies["ASP.NET_SessionId"];
        if (string.IsNullOrEmpty(sessao)) return AuthenticateResult.NoResult();

        var chave = "ponte:" + sessao;
        if (!cache.TryGetValue(chave, out UsuarioLegado? usuario))
        {
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
            // só resposta positiva vai para o cache (POC-02, achado 2)
            if (usuario is not null && Options.CacheSegundos > 0)
                cache.Set(chave, usuario, TimeSpan.FromSeconds(Options.CacheSegundos));
        }
        if (usuario is null) return AuthenticateResult.NoResult();

        var id = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Codigo.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim("login", usuario.Login),
            new Claim("perfil", usuario.Perfil.ToString()),
        }, PonteSessao.Esquema);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(id), PonteSessao.Esquema));
    }

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
