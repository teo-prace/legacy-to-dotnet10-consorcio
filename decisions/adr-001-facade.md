# ADR-001 · A .NET 10 facade in front of the legacy system

**Status:** accepted · **Evidence:** [spike 01](../spikes/01-facade) (8 tests)

## Context

SGC is a WebForms site (.NET Framework 4.8) with a classic ASP portal and VB6 components. New .NET 10
screens have to live next to it **on the same URL**, so users don't notice the switch, and there has to
be an immediate way back if a new screen misbehaves.

## Decision

An ASP.NET Core (.NET 10) app with **YARP** receives all traffic.

- A catch-all route (`Order = int.MaxValue`) forwards to the legacy system everything the new side
  doesn't serve yet.
- Every migrated screen gets a **configuration switch** (`Migracao:*`), read through `IOptionsMonitor`:
  turning it on or off needs no deploy and no restart.
- The old URL (`/Cobranca/Parcelas.aspx`) can be served by the new screen (path rewrite at the facade),
  so menus and links inside the legacy system don't change.
- The facade adds headers the legacy system doesn't send (`X-Frame-Options`, `frame-ancestors`, see
  ADR-003) and a diagnostic header (`X-SGC-Origem`).

## Alternatives

| Alternative | Why not |
|---|---|
| Rewrite everything and switch at once | The pattern behind the failures ([research](failed-migrations.md)) |
| Routing in IIS (ARR / URL Rewrite) | Works, but per-screen switches, identity (ADR-002) and headers would live in IIS configuration with no automated tests; with YARP they are testable .NET code |
| A new subdomain for new screens | Two origins: cookies, session and links between the two halves become a problem (spike 03, scenario 9) |

## Consequences

- ✅ 26/26 legacy screens work through the facade (postback, ViewState, redirects, popups, cookie).
- ✅ Measured cost: +0.47 ms per request (median).
- ✅ Rollback in ~220 ms in the same process; own 503 page if the legacy system goes down.
- ⚠️ The facade becomes the single entry point: two instances, monitoring and HTTPS terminated there.
- ⚠️ The legacy site must only be reachable through the facade (firewall or local binding).
- Technical details that become product rules: explicit `UseRouting()` after `UseStaticFiles()`;
  `MapStaticAssets()` for Blazor; YARP forwards the destination `Host`.
