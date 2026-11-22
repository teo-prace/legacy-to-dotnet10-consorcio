using Yarp.ReverseProxy.Forwarder;

namespace Sgc.Novo;

/// <summary>
/// Which screens are served by the new system. "legado" (default) or "novo", per screen, read on every
/// request: changing appsettings.json or an environment variable switches without a restart.
/// </summary>
public class MigrationSwitches
{
    public string Parcelas { get; set; } = "legado";
    public string Baixa { get; set; } = "legado";

    /// <summary>"TELA NOVA" badge on new screens: on in staging and during the pilot, off when the switch should go unnoticed.</summary>
    public bool SeloTelaNova { get; set; } = true;

    public bool IsNew(string value) => string.Equals(value, "novo", StringComparison.OrdinalIgnoreCase);
}

/// <summary>When the legacy site doesn't answer, a friendly 503 instead of an empty 502 (spike 01).</summary>
public static class LegacyDownPage
{
    public static async Task InvokeAsync(HttpContext ctx, Func<Task> next)
    {
        await next();
        var error = ctx.GetForwarderErrorFeature();
        if (error is null || ctx.Response.HasStarted) return;
        ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        ctx.Response.ContentType = "text/html; charset=utf-8";
        ctx.Response.Headers["X-SGC-Origem"] = "fachada";
        await ctx.Response.WriteAsync("<html><body><h1>SGC temporariamente indisponível</h1>" +
            "<p>O sistema não respondeu. Tente novamente em instantes.</p></body></html>");
    }
}
