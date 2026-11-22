// POC-02a · Identidade/sessão pelos System.Web Adapters oficiais (remote session)
// Lado .NET Framework: cópia do legado com os adaptadores (pocs/poc02a-adaptadores/legado-alteracoes.md)
using Microsoft.AspNetCore.SystemWebAdapters;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

builder.Services.AddReverseProxy().LoadFromConfig(cfg.GetSection("ReverseProxy"));
builder.Services.AddSystemWebAdapters()
    .AddJsonSessionSerializer(o =>
    {
        o.RegisterKey<int>("CD_USUARIO");
        o.RegisterKey<string>("NM_USUARIO");
        o.RegisterKey<int>("ID_PERFIL");
    })
    .AddRemoteAppClient(o =>
    {
        o.RemoteAppUrl = new Uri(cfg["Legado:Url"]!);
        o.ApiKey = cfg["Legado:ApiKey"]!;
    })
    .AddSessionClient();

// sem isso, qualquer chave de sessao do legado nao registrada (ex.: o DataRow 'Usuario') derruba a leitura inteira
builder.Services.Configure<Microsoft.AspNetCore.SystemWebAdapters.SessionState.Serialization.SessionSerializerOptions>(o => o.ThrowOnUnknownSessionKey = false);

var app = builder.Build();
app.UseSystemWebAdapters();

app.MapGet("/novo/ping", () => "ok");

app.MapGet("/novo/quem-sou-eu", (HttpContext c) =>
{
    var s = c.AsSystemWeb().Session;
    if (s?["CD_USUARIO"] is null) return Results.Redirect("/Login.aspx?ReturnUrl=%2fnovo%2fquem-sou-eu");
    return Results.Json(new { codigo = s["CD_USUARIO"], nome = s["NM_USUARIO"], perfil = s["ID_PERFIL"] });
}).RequireSystemWebAdapterSession(new SessionAttribute { SessionBehavior = System.Web.SessionState.SessionStateBehavior.ReadOnly });

app.MapReverseProxy();
app.Run();

