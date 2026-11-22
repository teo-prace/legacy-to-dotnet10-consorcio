using System.Diagnostics;
using System.Net;

namespace Sgc.Tests.Infra;

/// <summary>
/// Starts what the tests need (legacy site on IIS Express, .NET apps) when it isn't running already,
/// and stops what it started at the end. Ports: legacy 8090, spikes 81xx, modern 82xx.
/// </summary>
public static class TestEnvironment
{
    public static readonly string Root = FindRoot();
    public const string LegacyUrl = "http://localhost:8090";
    public const string ConnectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=SGC;Integrated Security=True;TrustServerCertificate=True";

    static readonly HttpClient Http = new(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(15) };
    static readonly List<Process> Started = new();
    static readonly object Gate = new();

    static TestEnvironment() => AppDomain.CurrentDomain.ProcessExit += (_, _) => StopAll();

    static string FindRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !(Directory.Exists(Path.Combine(d.FullName, "legacy")) && Directory.Exists(Path.Combine(d.FullName, "tests"))))
            d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("repository root not found");
    }

    public static string PathFromRoot(params string[] parts) => Path.Combine([Root, .. parts]);

    public static async Task EnsureLegacyAsync()
    {
        if (await RespondsAsync(LegacyUrl + "/Login.aspx")) return;
        await StartIisExpressAsync(PathFromRoot("legacy", "src", "SGC.Web"), 8090);
    }

    /// <summary>
    /// IIS Express with its output drained. Without draining, the pipe fills up (one line per request)
    /// and IIS Express hangs; without redirecting, it inherits the test host's output and the test run
    /// never ends.
    /// </summary>
    public static async Task StartIisExpressAsync(string folder, int port)
    {
        var iis = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "IIS Express", "iisexpress.exe");
        Start(iis, $"/path:\"{folder}\" /port:{port} /clr:v4.0", new Dictionary<string, string> { ["Urls"] = $"http://localhost:{port}" });
        await WaitAsync($"http://localhost:{port}/Login.aspx", TimeSpan.FromSeconds(180));
    }

    /// <summary>Starts an already built .NET app on a port. Extra variables override its configuration.</summary>
    public static async Task<string> StartAppAsync(string projectFolder, string dll, int port, IDictionary<string, string>? env = null, string healthPath = "/novo/ping")
    {
        var url = $"http://localhost:{port}";
        var bin = Path.Combine(projectFolder, "bin", "Debug", "net10.0");
        var vars = new Dictionary<string, string> { ["Urls"] = url, ["ASPNETCORE_ENVIRONMENT"] = "Production" };
        if (env is not null) foreach (var kv in env) vars[kv.Key] = kv.Value;
        Start("dotnet", $"\"{Path.Combine(bin, dll)}\"", vars, bin);
        await WaitAsync(url + healthPath, TimeSpan.FromSeconds(60));
        return url;
    }

    static void Start(string exe, string args, IDictionary<string, string>? env, string? folder = null)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = folder ?? Root,
        };
        if (env is not null) foreach (var kv in env) psi.Environment[kv.Key] = kv.Value;
        var p = Process.Start(psi)!;
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        lock (Gate) Started.Add(p);
    }

    public static async Task<bool> RespondsAsync(string url)
    {
        try { using var r = await Http.GetAsync(url); return (int)r.StatusCode < 500; }
        catch { return false; }
    }

    static async Task WaitAsync(string url, TimeSpan limit)
    {
        var end = DateTime.UtcNow + limit;
        while (DateTime.UtcNow < end)
        {
            if (await RespondsAsync(url)) return;
            await Task.Delay(500);
        }
        throw new TimeoutException("no response from " + url);
    }

    public static void Stop(int port)
    {
        lock (Gate)
            foreach (var p in Started.Where(p => !p.HasExited && p.StartInfo.Environment.TryGetValue("Urls", out var u) && u == $"http://localhost:{port}").ToList())
            { try { p.Kill(true); } catch { } Started.Remove(p); }
    }

    public static void StopAll()
    {
        lock (Gate)
            foreach (var p in Started) { try { if (!p.HasExited) p.Kill(true); } catch { } }
    }

    public static HttpClient Client(bool followRedirects = false, CookieContainer? cookies = null) =>
        new(new HttpClientHandler { AllowAutoRedirect = followRedirects, CookieContainer = cookies ?? new CookieContainer(), UseCookies = true })
        { Timeout = TimeSpan.FromSeconds(30) };
}
