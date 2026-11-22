// POC-01 · Fachada YARP na frente do WebForms
//
// Hipotese: um ASP.NET Core (.NET 10) na frente, repassando tudo para o legado,
// nao quebra nenhuma tela (login, postback/ViewState, redirects, cookies, boleto
// em nova janela), e permite "ligar" uma rota nova e desligar de volta (rollback)
// sem reiniciar.
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(ctx =>
    {
        // marca a origem da resposta (ajuda a depurar e a testar)
        ctx.AddResponseHeader("X-SGC-Origem", "legado", append: false);
    });
builder.Services.Configure<ChavesMigracao>(builder.Configuration.GetSection("Migracao"));

var app = builder.Build();

app.MapGet("/novo/ping", (HttpContext c) =>
{
    c.Response.Headers["X-SGC-Origem"] = "novo";
    return Results.Text("SGC.Novo .NET " + Environment.Version);
});

// Rota "migrada" controlada por chave (rollback = desligar a chave no appsettings, sem restart)
app.Use(async (ctx, next) =>
{
    var chaves = ctx.RequestServices.GetRequiredService<IOptionsMonitor<ChavesMigracao>>().CurrentValue;
    if (chaves.ParcelasNoNovo && ctx.Request.Path.Equals("/Cobranca/Parcelas.aspx", StringComparison.OrdinalIgnoreCase))
    {
        ctx.Response.Headers["X-SGC-Origem"] = "novo";
        ctx.Response.ContentType = "text/html; charset=utf-8";
        await ctx.Response.WriteAsync("<html><body><h1>Parcelas (SGC.Novo)</h1><p>Tela atendida pelo .NET 10.</p></body></html>");
        return;
    }
    await next();
});

app.MapReverseProxy(proxy =>
{
    // legado fora do ar: pagina amigavel em vez de 502 vazio
    proxy.Use(async (ctx, next) =>
    {
        await next();
        var erro = ctx.GetForwarderErrorFeature();
        if (erro is not null && !ctx.Response.HasStarted)
        {
            ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            ctx.Response.ContentType = "text/html; charset=utf-8";
            ctx.Response.Headers["X-SGC-Origem"] = "fachada";
            await ctx.Response.WriteAsync("<html><body><h1>SGC temporariamente indisponível</h1>" +
                $"<p>O sistema legado não respondeu ({erro.Error}). Tente novamente em instantes.</p></body></html>");
        }
    });
});

app.Run();

public class ChavesMigracao
{
    public bool ParcelasNoNovo { get; set; }
}
