# ADR-004 · UI technology for migrated screens: Blazor static SSR

**Status:** accepted · **Evidence:** [spike 04](../spikes/04-ui) (7 tests)

## Context

The same real legacy screen (`Cobranca/Parcelas.aspx`: filters, grid, paging, links) was built three
times, with the legacy CSS and the identity from ADR-002, and compared row by row with the legacy screen.

| | Razor Pages | Blazor static SSR | Blazor Interactive Server |
|---|---|---|---|
| Parity with the legacy screen | ✅ | ✅ | ✅ |
| Refresh, deep link, Back with filters | ✅ | ✅ | 🔴 lost |
| Without JavaScript | ✅ | ✅ | 🔴 |
| Deploy with a user on the screen | ✅ | ✅ | 🔴 circuit drops, back to initial state |
| First load | 3.4 KB, 2 requests | 222.6 KB (196 KB of `blazor.web.js`, cached) | 204.5 KB + WebSocket |
| Database queries per load | 1 | 1 | 2 (prerender + circuit) |
| Needs in production | HTTP | HTTP | WebSocket, affinity, memory per user |

## Decision

- **Migrated screens use Blazor with static server rendering (SSR)**, GET forms with the filter in the
  URL, and the legacy layout and CSS during coexistence.
- **Blazor Interactive Server is not the default.** Back-office screens coming from WebForms depend on
  refresh, deep links, Back and on deploys that don't kick users out. Allowed only for isolated
  components without state the user can't afford to lose.
- **Razor Pages** behaves the same and is lighter (3.4 KB); its `OnGet`/`OnPost` model is the closest to
  WebForms code-behind. It stays as the equivalent alternative. Blazor SSR won on reusable components
  (the grid is one) and interactive islands without a second framework.

## Rules for every migrated screen

- **A parity test against the legacy screen before the switch is turned on.** The spike found a bug this
  way: "status = all" became `null` in model binding and fell back to the default filter, showing 12
  rows instead of 50, with no error.
- Same `estilo.css` as the legacy system during coexistence; the redesign comes later, once.
- Blazor links to legacy pages with `data-enhance-nav="false"`.
