// POC-04 · Razor Pages x Blazor SSR estático x Blazor Interactive Server, com a mesma tela real
// (Cobranca/Parcelas.aspx), o mesmo layout/CSS do legado e a identidade pela ponte de sessão (POC-02).
//
//   /novo/rp/parcelas    Razor Pages
//   /novo/ssr/parcelas   Blazor, renderização estática no servidor
//   /novo/int/parcelas   Blazor Interactive Server (SignalR)
//
// Migracao:ParcelasAspx = legado | rp | ssr | int  -> quem atende a URL antiga /Cobranca/Parcelas.aspx
using Microsoft.Extensions.Options;
using Poc04.Ui.Components;
using Poc04.Ui.Dados;
using Pocs.Compartilhado;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;
// rodando da pasta bin fora de Development (como nos testes), sem isto o _framework/blazor.web.js
// não é servido e o pedido cai na rota "pega-tudo" do legado
builder.WebHost.UseStaticWebAssets();

builder.Services.AddPonteSessao(cfg);
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddRazorPages();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddScoped<ConsultaParcelas>();
builder.Services.Configure<ChavesMigracao>(cfg.GetSection("Migracao"));
builder.Services.AddReverseProxy().LoadFromConfig(cfg.GetSection("ReverseProxy"));

var app = builder.Build();

// a URL antiga passa a ser atendida pela tela nova escolhida (os links do legado continuam valendo)
app.Use(async (ctx, next) =>
{
    var destino = ctx.RequestServices.GetRequiredService<IOptionsMonitor<ChavesMigracao>>().CurrentValue.ParcelasAspx;
    if (destino != "legado" && ctx.Request.Path.Equals("/Cobranca/Parcelas.aspx", StringComparison.OrdinalIgnoreCase))
        ctx.Request.Path = $"/novo/{destino}/parcelas";
    await next();
});

app.UseStaticFiles();
app.UseRouting();   // depois dos estáticos e da troca de rota (POC-03, achado 2)
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/novo/ping", () => "ok");
app.MapGet("/novo/diag/consultas-int", () => Poc04.Ui.Components.Pages.ParcelasInterativo.ConsultasExecutadas);   // só na POC
app.MapStaticAssets();
app.MapRazorPages();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapReverseProxy();
app.Run();

public class ChavesMigracao
{
    public string ParcelasAspx { get; set; } = "legado";
}
