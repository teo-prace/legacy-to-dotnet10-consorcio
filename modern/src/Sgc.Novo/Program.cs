// SGC.Novo: the .NET 10 side of the CeCons system.
//
// One process plays three roles while the legacy system is still running:
//   1. facade (ADR-001): every request comes here; what the new side doesn't serve goes to the legacy site
//   2. identity (ADR-002): new screens read who is logged in from the legacy session, through the bridge
//   3. new screens (ADR-004): Blazor static SSR, switched on per screen (Migracao:*), same URLs as before
using Microsoft.Extensions.Options;
using Sgc.Novo;
using Sgc.Novo.Billing;
using Sgc.Novo.Components;
using Sgc.Novo.Data;
using Sgc.Novo.Identity;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// served from bin outside Development (tests, container): without this, _framework/blazor.web.js isn't
// found and the request falls into the catch-all route to the legacy system (spike 04, finding 4)
builder.WebHost.UseStaticWebAssets();

builder.Services.AddSessionBridge(cfg);
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddRazorComponents();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<BillingParameters>();
builder.Services.AddScoped<InstallmentQueries>();
builder.Services.AddScoped(_ => new PaymentPosting(cfg.GetConnectionString("SGC")!));
builder.Services.Configure<MigrationSwitches>(cfg.GetSection("Migracao"));
builder.Services.AddReverseProxy()
    .LoadFromConfig(cfg.GetSection("ReverseProxy"))
    .AddTransforms(c => c.AddResponseHeader("X-SGC-Origem", "legado", append: false));

var app = builder.Build();

// Anti-framing on every response, legacy or new: the legacy site never sent these headers and could be
// framed by any site (spike 03, ADR-003).
app.Use(async (ctx, next) =>
{
    ctx.Response.OnStarting(() =>
    {
        ctx.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        ctx.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'self'";
        ctx.Response.Headers.TryAdd("X-SGC-Origem", "novo");
        return Task.CompletedTask;
    });
    await next();
});

// The old URLs are served by the new screens when their switch is on. Links and menus inside the legacy
// system keep working, and turning a switch off is the rollback (no deploy, no restart).
app.Use(async (ctx, next) =>
{
    var switches = ctx.RequestServices.GetRequiredService<IOptionsMonitor<MigrationSwitches>>().CurrentValue;
    var path = ctx.Request.Path;
    if (switches.IsNew(switches.Parcelas) && path.Equals("/Cobranca/Parcelas.aspx", StringComparison.OrdinalIgnoreCase))
        ctx.Request.Path = "/novo/parcelas";
    else if (switches.IsNew(switches.Baixa) && path.Equals("/Cobranca/Baixa.aspx", StringComparison.OrdinalIgnoreCase))
        ctx.Request.Path = "/novo/baixa";
    await next();
});

app.UseStaticFiles();
app.UseRouting();   // after static files and the path rewrite, or the catch-all route wins (spike 03, finding 2)
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/novo/ping", () => "SGC.Novo .NET " + Environment.Version);
app.Map("/Sessao/{**rest}", () => Results.NotFound());   // the session bridge is internal (ADR-002)
app.MapStaticAssets();
app.MapRazorComponents<App>();
app.MapReverseProxy(proxy => proxy.Use(LegacyDownPage.InvokeAsync));

app.Run();

/// <summary>Exposed for tests.</summary>
public partial class Program;
