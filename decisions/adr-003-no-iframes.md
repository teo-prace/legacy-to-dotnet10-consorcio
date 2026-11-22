# ADR-003 · No iframes for coexistence; anti-framing headers now

**Status:** accepted · **Evidence:** [spike 03](../spikes/03-iframe) (9 tests)

## Context

A common way to make old and new screens coexist is a new shell that shows legacy pages inside an
iframe (or the reverse). That raises questions about security (clickjacking, sandbox), popups,
cross-window messaging, session, refresh and the back button. The spike tested each one.

## Decision

1. **No iframes for coexistence.** The facade (ADR-001) already serves new and old screens on the same
   origin, as full pages: deep links, refresh, back, popups and session work with no extra code.
2. **Send `X-Frame-Options: SAMEORIGIN` and `Content-Security-Policy: frame-ancestors 'self'`** on every
   response, from the facade. Today **any site can frame the legacy system** (clickjacking). This goes
   to production in the first phase, before any new screen.

**Acceptable exception:** a small new widget, no internal navigation, inside a legacy page, same origin,
with `postMessage` validating origin and message format.

## What the spike measured

| Scenario | With an iframe |
|---|---|
| WebForms postback inside the frame | works |
| Refresh and deep link | break; fixable by mirroring the inner page in the `#hash` |
| Back button | **no clean fix** |
| Expired session | the login opens inside the frame; fixable with detection in the shell |
| Boleto popup with `sandbox` | blocked; and `allow-scripts` + `allow-same-origin` (required by WebForms) cancel the sandbox |
| Frame from another site | impossible without `SameSite=None` (opens CSRF) |
| `postMessage` forged by another site | ignored, with validation |

Four mitigations to maintain, one without a solution, and nothing the facade doesn't already provide.

## Consequences

- ✅ An existing vulnerability fixed without touching the legacy system.
- ✅ No URL, history or height synchronization code to maintain.
- ⚠️ Visual consistency comes from using the same layout and CSS on new screens (ADR-004).
