// POC-03 · iframe: o .NET 10 hospedando telas do legado (e vice-versa)
//
// Perguntas: o que quebra quando uma tela vive dentro de um iframe? Segurança (clickjacking),
// cookies, popups, confirm(), sessão expirada, link direto/F5, botão voltar, postMessage.
//
// /novo/shell.html      shell novo com o legado num iframe (mesma origem, via fachada)
// /novo/sandbox.html    o mesmo, com iframe sandbox sem allow-popups
// /novo/filho.html      página nova pensada para viver num iframe (fala com o pai por postMessage)
// /novo/externo.html    página "de outro site" que tenta emoldurar o SGC (abrir por 127.0.0.1)
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);
var protegerFraming = builder.Configuration.GetValue("Fachada:ProtegerFraming", true);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(c =>
    {
        if (protegerFraming)
        {
            // o legado não manda nenhum cabeçalho anti-framing: a fachada passa a mandar por ele
            c.AddResponseHeader("X-Frame-Options", "SAMEORIGIN", append: false);
            c.AddResponseHeader("Content-Security-Policy", "frame-ancestors 'self'", append: false);
        }
    });

var app = builder.Build();

app.Use(async (ctx, next) =>
{
    if (protegerFraming && ctx.Request.Path.StartsWithSegments("/novo"))
    {
        ctx.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        ctx.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'self'";
    }
    await next();
});
app.UseDefaultFiles();
app.UseStaticFiles();
// ACHADO: sem UseRouting explícito aqui, o roteamento roda no início do pipeline, a rota "pega-tudo"
// do YARP casa com /novo/shell.html e o middleware de estáticos não serve arquivo com endpoint escolhido:
// o pedido ia para o legado (404 do IIS).
app.UseRouting();
app.MapGet("/novo/ping", () => "ok");
app.MapReverseProxy();
app.Run();
