using System.Globalization;
using System.Net;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Playwright;
using Sgc.Novo.Billing;
using Sgc.Tests.Infra;
using Sgc.Tests.Spikes;
using Xunit.Abstractions;

namespace Sgc.Tests.Modern;

/// <summary>
/// SGC.Novo running in front of the legacy site: the facade, the switches, the new screens compared with the
/// legacy ones, and a payment posted end to end on a database copy. Ports 82xx.
/// </summary>
[Collection("poc06")]
[Trait("Area", "Modern")]
public class ModernAppTests(BancoPoc06 database, BrowserFixture browser, ITestOutputHelper output)
{
    static readonly CultureInfo PtBr = new("pt-BR");

    static async Task<string> StartAsync(int port, Dictionary<string, string>? env = null)
    {
        await TestEnvironment.EnsureLegacyAsync();
        return await TestEnvironment.StartAppAsync(TestEnvironment.PathFromRoot("modern", "src", "Sgc.Novo"), "Sgc.Novo.dll", port, env);
    }

    static async Task<List<string>> NossosNumerosAsync(IPage p, string cellSelector) =>
        (await p.Locator(cellSelector).AllInnerTextsAsync()).Select(t => t.Trim()).Where(t => t.Length > 0).ToList();

    [Fact(DisplayName = "M-4 · facade: legacy screens pass through with anti-framing headers; the session bridge is not reachable")]
    public async Task Facade()
    {
        var url = await StartAsync(8201);
        try
        {
            using var http = TestEnvironment.Client();
            var r = await http.GetAsync(url + "/Login.aspx");
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Equal("legado", r.Headers.GetValues("X-SGC-Origem").Single());
            Assert.Equal("SAMEORIGIN", r.Headers.GetValues("X-Frame-Options").Single());
            Assert.Equal("frame-ancestors 'self'", r.Headers.GetValues("Content-Security-Policy").Single());

            // the Blazor script is served by the new side, not forwarded to the legacy site (spike 04, finding 4)
            var script = await http.GetAsync(url + "/_framework/blazor.web.js");
            Assert.Equal(HttpStatusCode.OK, script.StatusCode);
            Assert.Equal("novo", script.Headers.GetValues("X-SGC-Origem").Single());

            var bridge = new HttpRequestMessage(HttpMethod.Get, url + "/Sessao/Quem.ashx");
            bridge.Headers.Add("X-SGC-Ponte", "dev-ponte-7f3a9c2e-troque-em-producao");
            Assert.Equal(HttpStatusCode.NotFound, (await http.SendAsync(bridge)).StatusCode);

            // with every switch off, the old URLs are served by the legacy system
            var page = await browser.NewPageAsync();
            await BrowserFixture.LoginAsync(page, url);
            await page.GotoAsync(url + "/Cobranca/Parcelas.aspx");
            Assert.Null(await page.GetAttributeAsync("body", "data-tecnologia"));
            Assert.Equal(0, await page.Locator(".seloTelaNova").CountAsync());
        }
        finally { TestEnvironment.Stop(8201); }
    }

    [Fact(DisplayName = "M-5 · legacy down: friendly 503 page from the facade, new routes still up")]
    public async Task LegacyDown()
    {
        var url = await StartAsync(8204, new() { ["ReverseProxy__Clusters__legado__Destinations__iis__Address"] = "http://localhost:8299/" });
        try
        {
            using var http = TestEnvironment.Client();
            var r = await http.GetAsync(url + "/Login.aspx");
            Assert.Equal(HttpStatusCode.ServiceUnavailable, r.StatusCode);
            Assert.Contains("temporariamente indisponível", await r.Content.ReadAsStringAsync());
            Assert.Contains(".NET", await http.GetStringAsync(url + "/novo/ping"));
        }
        finally { TestEnvironment.Stop(8204); }
    }

    [Fact(DisplayName = "M-6 · installments: with the switch on, the old URL is served by the new screen and returns the same rows as the legacy one")]
    public async Task InstallmentsParity()
    {
        var url = await StartAsync(8202, new() { ["Migracao__Parcelas"] = "novo" });
        try
        {
            var page = await browser.NewPageAsync();
            await BrowserFixture.LoginAsync(page, url);
            var legacy = await browser.NewPageAsync();
            await BrowserFixture.LoginAsync(legacy, TestEnvironment.LegacyUrl);

            // the menu inside the legacy system leads to the new screen, badge included
            await page.ClickAsync("a:text-is('Parcelas')");
            await page.WaitForLoadStateAsync();
            Assert.Equal("blazor", await page.GetAttributeAsync("body", "data-tecnologia"));
            Assert.EndsWith("/Cobranca/Parcelas.aspx", page.Url);
            Assert.Contains("TELA NOVA", await page.InnerTextAsync(".seloTelaNova"));

            foreach (var (group, status, nn) in new[] { ("4", "V", ""), ("4", "3", ""), ("4", "2", ""), ("4", "", "00000022"), ("1", "1", "") })
            {
                await legacy.GotoAsync(TestEnvironment.LegacyUrl + "/Cobranca/Parcelas.aspx");
                await legacy.SelectOptionAsync("#ctl00_cphConteudo_ddlGrupo", group);
                await legacy.SelectOptionAsync("#ctl00_cphConteudo_ddlSituacao", status);
                await legacy.FillAsync("#ctl00_cphConteudo_txtNossoNumero", nn);
                await Task.WhenAll(legacy.WaitForLoadStateAsync(LoadState.NetworkIdle), legacy.ClickAsync("#ctl00_cphConteudo_btnPesquisar"));
                var expected = await NossosNumerosAsync(legacy, "#ctl00_cphConteudo_gvParcelas > tbody > tr:not(.rodapeGrid) > td:nth-child(5)");

                await page.GotoAsync($"{url}/Cobranca/Parcelas.aspx?grupo={group}&situacao={status}&nn={nn}");
                var actual = await NossosNumerosAsync(page, "#grade td.nn");
                output.WriteLine($"group={group} status='{status}' nn='{nn}': legacy {expected.Count} rows, new {actual.Count} rows, {(actual.SequenceEqual(expected) ? "same" : "DIFFERENT")}");
                Assert.Equal(expected, actual);
            }
        }
        finally { TestEnvironment.Stop(8202); }
    }

    [Fact(DisplayName = "M-7 · payment on the new screen: fees from the new rule, posted through the legacy procedure, rounding recorded")]
    public async Task PaymentEndToEnd()
    {
        database.Restaurar();
        PaymentPostingTests.ApplyModernSchema();
        using var cn = new SqlConnection(BancoPoc06.Conexao());
        var target = cn.QueryFirst<(int Id, DateTime Due, decimal Value)>(
            "SELECT TOP 1 CD_PARCELA, DT_VENCIMENTO, VL_PARCELA FROM TB_PARCELA WHERE ID_SITUACAO = 2 AND DT_VENCIMENTO < DATEADD(DAY, -10, GETDATE()) ORDER BY DT_VENCIMENTO");
        var rule = new LateFeeCalculator(new Sgc.Novo.Billing.BusinessCalendar(cn.Query<DateTime>("SELECT DT_FERIADO FROM TB_FERIADO")));
        var expected = rule.Calculate(target.Value, target.Due, DateTime.Today);

        var url = await StartAsync(8203, new() { ["Migracao__Baixa"] = "novo", ["ConnectionStrings__SGC"] = BancoPoc06.Conexao() });
        try
        {
            // a read-only user sees the installment but can't post
            var readOnly = await browser.NewPageAsync();
            await BrowserFixture.LoginAsync(readOnly, url, "consulta");
            await readOnly.GotoAsync($"{url}/Cobranca/Baixa.aspx?id={target.Id}");
            Assert.Equal("Emitida", await readOnly.InnerTextAsync("#situacao"));
            Assert.Equal(0, await readOnly.Locator("#confirmar").CountAsync());

            var page = await browser.NewPageAsync();
            page.Dialog += async (_, d) => await d.AcceptAsync();   // "Confirma a baixa da parcela?"
            await BrowserFixture.LoginAsync(page, url);
            await page.GotoAsync($"{url}/Cobranca/Baixa.aspx?id={target.Id}");
            Assert.Equal("blazor", await page.GetAttributeAsync("body", "data-tecnologia"));
            var fine = decimal.Parse((await page.InnerTextAsync("#multa")).Replace("R$", "").Trim(), PtBr);
            var interest = decimal.Parse((await page.InnerTextAsync("#juros")).Split('(')[0].Replace("R$", "").Trim(), PtBr);
            output.WriteLine($"installment {target.Value:N2} due {target.Due:dd/MM/yyyy}: screen {fine:N2} + {interest:N2}, rule {expected.Fine:N2} + {expected.Interest:N2}");
            Assert.Equal((expected.Fine, expected.Interest), (fine, interest));

            await Task.WhenAll(page.WaitForLoadStateAsync(LoadState.NetworkIdle), page.ClickAsync("#confirmar"));
            await page.WaitForSelectorAsync("#mensagem");
            Assert.Equal("Parcela baixada.", (await page.InnerTextAsync("#mensagem")).Trim());
            Assert.StartsWith("Paga", await page.InnerTextAsync("#situacao"));

            var posted = cn.QuerySingle<(int Status, decimal Fine, decimal Interest, decimal Paid)>(
                "SELECT ID_SITUACAO, VL_MULTA, VL_JUROS, VL_PAGO FROM TB_PARCELA WHERE CD_PARCELA = @id", new { id = target.Id });
            Assert.Equal((3, expected.Fine, expected.Interest, expected.Total(target.Value)), posted);
            Assert.Equal(3, cn.ExecuteScalar<int>("SELECT COUNT(*) FROM TB_GRUPO_MOVIMENTO WHERE CD_PARCELA = @id AND ID_TIPO_MOVIMENTO = 1", new { id = target.Id }));
            Assert.Equal(1, cn.ExecuteScalar<int>("SELECT COUNT(*) FROM novo.TB_ENCARGO_ARREDONDAMENTO WHERE CD_PARCELA = @id", new { id = target.Id }));

            // posting again (stale screen): the procedure refuses, the user is told, nothing is written twice
            await page.GoBackAsync();
            await page.GotoAsync($"{url}/Cobranca/Baixa.aspx?id={target.Id}");
            Assert.Equal(0, await page.Locator("#confirmar").CountAsync());
        }
        finally { TestEnvironment.Stop(8203); }
    }
}
