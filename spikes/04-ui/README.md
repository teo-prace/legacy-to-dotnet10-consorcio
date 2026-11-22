# Spike 04 · Razor Pages, Blazor static SSR or Blazor Interactive Server?

**Question:** which .NET 10 UI technology should the migrated screens use?

**Setup:** the **same real legacy screen** (`Cobranca/Parcelas.aspx`: filters, grid, paging, links to
boleto and payment) built three times in one app, with the **legacy layout and CSS** (served by the
legacy system through the facade) and the identity from spike 02.

| Route | Technology |
|---|---|
| `/novo/rp/parcelas` | Razor Pages (GET form, `PageModel`) |
| `/novo/ssr/parcelas` | Blazor, static server rendering (GET form) |
| `/novo/int/parcelas` | Blazor Interactive Server (state in the circuit, events over SignalR) |

Tests: `tests/Sgc.Tests/Spikes/Spike04UiTests.cs` (7/7).

## Result

| | Razor Pages | Blazor static SSR | Blazor Interactive Server |
|---|---|---|---|
| **Parity with the legacy screen** (5 filters, row by row, same order) | ✅ | ✅ | ✅ |
| **Refresh, deep link, Back** after filtering and paging | ✅ (filter in the URL) | ✅ | 🔴 loses filter and page (state only in the circuit) |
| **Without JavaScript** | ✅ | ✅ | 🔴 first load only |
| **Deploy with a user on the screen** | ✅ carries on | ✅ carries on | 🔴 the circuit dies, the page reloads to its initial state |
| **First load** | 2 requests, **3.4 KB** | 5 requests, 222.6 KB (`blazor.web.js`: 196 KB, cached) | 6 requests, 204.5 KB + a WebSocket |
| **Database queries per load** | 1 | 1 | **2** (prerender + circuit) |
| Old URL `/Cobranca/Parcelas.aspx` served by the new screen | ✅ | ✅ (tested) | ✅ |
| Needs in production | HTTP | HTTP | WebSocket through the facade, session affinity with more than one server, memory per connected user |

## Findings

1. **The parity test caught a bug in the new screen.** With "status = all" (`situacao=` empty), ASP.NET
   Core binding turned the empty string into `null` and the screen fell back to the default filter
   (overdue): 12 rows instead of 50, **no error**. Exactly the kind of silent divergence that destroys
   trust in a migration. Fixed (explicit value `T`; empty from an old URL means all).
   Lesson: **every migrated screen gets a parity test against the legacy screen before its switch is
   turned on.**
2. **The old URL is served by the new screen** (path rewrite at the facade): menus and links inside the
   legacy system keep working with no change to `Site.master`.
3. **One CSS source during coexistence:** users don't notice the technology change; the visual
   redesign comes later, once.
4. Running from `bin` outside Development, Blazor needs `UseStaticWebAssets()` and `MapStaticAssets()`,
   or `blazor.web.js` falls into the catch-all route and goes to the legacy system.
5. Blazor links to legacy pages need `data-enhance-nav="false"`.

Decision: [ADR-004](../../decisions/adr-004-ui.md).
