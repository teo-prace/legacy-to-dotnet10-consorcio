using Microsoft.Playwright;

namespace Sgc.Tests.Infra;

/// <summary>The Edge installed on the machine (msedge channel), headless. Nothing is downloaded.</summary>
public sealed class BrowserFixture : IAsyncLifetime
{
    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new() { Channel = "msedge", Headless = true });
    }

    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        Playwright.Dispose();
    }

    public async Task<IPage> NewPageAsync()
    {
        var ctx = await Browser.NewContextAsync(new() { Locale = "pt-BR" });
        return await ctx.NewPageAsync();
    }

    /// <summary>Logs into the back office at the given base URL (legacy directly or through the facade).</summary>
    public static async Task LoginAsync(IPage page, string baseUrl, string user = "msouza")
    {
        await page.GotoAsync(baseUrl + "/Login.aspx");
        await page.FillAsync("#txtLogin", user);
        await page.FillAsync("#txtSenha", "cecons123");
        await Task.WhenAll(page.WaitForURLAsync("**/Default.aspx"), page.ClickAsync("#btnEntrar"));
    }
}

[CollectionDefinition("browser")]
public class BrowserCollection : ICollectionFixture<BrowserFixture> { }
