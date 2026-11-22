using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Sgc.Novo.Identity;

/// <summary>
/// Who is logged in, read from the legacy session through Sessao/Quem.ashx (ADR-002). Login, logout and
/// timeout stay with the legacy system; this side only reads.
/// </summary>
public static class SessionBridge
{
    public const string Scheme = "LegacySession";

    /// <summary>Legacy profiles: 1 administrator, 2 operator, 3 read-only.</summary>
    public const string CanPostPayments = "CanPostPayments";

    public static IServiceCollection AddSessionBridge(this IServiceCollection services, IConfiguration cfg)
    {
        // no automatic default scheme: otherwise authentication runs on every request forwarded to the
        // legacy system (spike 02, finding 1)
        AppContext.SetSwitch("Microsoft.AspNetCore.Authentication.SuppressAutoDefaultScheme", true);
        services.AddMemoryCache();
        services.AddHttpClient("legacy", c => { c.BaseAddress = new Uri(cfg["Legado:Url"]!); c.Timeout = TimeSpan.FromSeconds(5); })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false });
        services.AddAuthentication(o => o.DefaultChallengeScheme = Scheme)
                .AddScheme<SessionBridgeOptions, SessionBridgeHandler>(Scheme, o =>
                {
                    o.Key = cfg["Legado:PonteChave"]!;
                    o.CacheSeconds = cfg.GetValue("Legado:PonteCacheSegundos", 5);
                });
        services.AddAuthorization(o =>
        {
            o.AddPolicy("LoggedIn", p => p.AddAuthenticationSchemes(Scheme).RequireAuthenticatedUser());
            o.AddPolicy(CanPostPayments, p => p.AddAuthenticationSchemes(Scheme).RequireClaim("perfil", "1", "2"));
        });
        return services;
    }
}

public class SessionBridgeOptions : AuthenticationSchemeOptions
{
    public string Key { get; set; } = "";
    public int CacheSeconds { get; set; } = 5;
}

public record LegacyUser(int Codigo, string Login, string Nome, int Perfil, int ExpiraEmMinutos);

public class SessionBridgeHandler(
    IOptionsMonitor<SessionBridgeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
    IHttpClientFactory http, IMemoryCache cache)
    : AuthenticationHandler<SessionBridgeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var session = Request.Cookies["ASP.NET_SessionId"];
        if (string.IsNullOrEmpty(session)) return AuthenticateResult.NoResult();

        var cacheKey = "bridge:" + session;
        if (!cache.TryGetValue(cacheKey, out LegacyUser? user))
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "Sessao/Quem.ashx");
            req.Headers.Add("Cookie", "ASP.NET_SessionId=" + session);
            req.Headers.Add("X-SGC-Ponte", Options.Key);
            try
            {
                using var resp = await http.CreateClient("legacy").SendAsync(req, Context.RequestAborted);
                user = resp.IsSuccessStatusCode ? await resp.Content.ReadFromJsonAsync<LegacyUser>() : null;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return AuthenticateResult.Fail("legacy system unavailable: " + ex.Message);
            }
            // only positive answers are cached: the legacy cookie exists before login, and a cached
            // "nobody" would send the user back to the login page in a loop (spike 02, finding 2)
            if (user is not null && Options.CacheSeconds > 0)
                cache.Set(cacheKey, user, TimeSpan.FromSeconds(Options.CacheSeconds));
        }
        if (user is null) return AuthenticateResult.NoResult();

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Codigo.ToString()),
            new Claim(ClaimTypes.Name, user.Nome),
            new Claim("login", user.Login),
            new Claim("perfil", user.Perfil.ToString()),
        ], SessionBridge.Scheme);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SessionBridge.Scheme));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Redirect("/Login.aspx?ReturnUrl=" + Uri.EscapeDataString(Request.PathBase + Request.Path + Request.QueryString));
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        return Response.WriteAsync("Acesso negado para o seu perfil.");
    }
}
