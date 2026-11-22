# Spike 03 · Coexisting through iframes?

**Question:** a common way to mix old and new screens is a new "shell" that shows legacy pages in an
iframe (or the other way around). What does that cost in security, popups, cross-window messaging,
session, refresh and the back button?

**Setup:** a facade on `:8103` that adds anti-framing headers and one on `:8113` without them; a shell
page, a sandboxed page, a child page and an "external site" page. Tests:
`tests/Sgc.Tests/Spikes/Spike03IframeTests.cs` (9/9).

## Result: it works, but every scenario needs a mitigation

| # | Scenario | Without mitigation | Mitigation tested |
|---|---|---|---|
| 1 | **Clickjacking** | 🔴 **Today the legacy system can be framed by any site** (no `X-Frame-Options`, no CSP) | The facade sends `X-Frame-Options: SAMEORIGIN` and `frame-ancestors 'self'`: the other site is blocked |
| 2 | Legacy page in the shell, same origin | ✅ Logged in, ViewState postbacks work inside the frame | Height adjusted to content (no double scroll) |
| 3 | Deep link and refresh | 🔴 The address bar doesn't change; refresh loses the page | The shell mirrors the inner page in the `#hash` |
| 4 | Back button | ⚠️ Frame navigations go into the window history; Back changes only the frame | No clean fix |
| 5 | Popup (boleto in a new window) | ✅ Works in a plain iframe | 🔴 Blocked with `sandbox` without `allow-popups`; and `allow-scripts` + `allow-same-origin` (which WebForms needs) cancel the sandbox anyway |
| 6 | `confirm()` inside the frame | ✅ Shown and respected | — |
| 7 | **Expired session** | 🔴 The legacy login opens **inside the frame** | The shell detects the login page, moves the whole window to it and comes back afterwards |
| 8 | `postMessage` | ✅ The child asks for height and navigation; the shell answers | `origin` and format validation: a forged message from another site is **ignored** |
| 9 | iframe from **another site** | 🔴 The `SameSite=Lax` session cookie is not sent: always the login page | No fix without weakening the cookie (`SameSite=None`) and opening CSRF |

## What it decides

**No iframes for coexistence.** The facade already gives the same origin with full-page navigation:
deep links, refresh, back, popups and session work with no extra code. The iframe would need four
mitigations of its own, one without a clean solution, and doesn't work across sites.

**Immediate win:** the anti-framing headers at the facade fix an exposure that exists today, before any
new screen ships.

Acceptable exception: a small new widget with no internal navigation inside a legacy page, same origin,
with validated `postMessage` (scenario 8).

Decision: [ADR-003](../../decisions/adr-003-no-iframes.md).
