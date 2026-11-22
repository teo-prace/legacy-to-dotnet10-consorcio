# Spike 01 · A .NET 10 facade in front of WebForms

**Question:** can an ASP.NET Core (.NET 10) app with YARP sit in front of the legacy system, forwarding
everything, without breaking anything? Can a new screen be switched on and back off instantly?

**Setup:** `Poc01.Fachada` on `http://localhost:8100`, a catch-all route to the WebForms site on IIS
Express (`http://localhost:8090`). **No change to the legacy system.**
Tests: `tests/Sgc.Tests/Spikes/Spike01FacadeTests.cs` (Playwright on the installed Edge + HttpClient).

## Result

| # | Scenario | Result |
|---|---|---|
| 01a | 26 legacy screens, logged in, through the facade | 26/26 served by the legacy system (`X-SGC-Origem: legado` header) |
| 01b | Postback with ViewState and `AutoPostBack` (quota simulation) | Works; same values as direct (R$ 2,346.67 / 1,546.67) |
| 01c | Redirects (no session, login, logout through a `LinkButton`) | The legacy system sends relative `Location` headers; the internal port never leaks |
| 01d | Boleto in a new window (`target="_blank"`) | Opens through the facade with a valid typeable line |
| 01e | Legacy session cookie | `ASP.NET_SessionId` issued for the facade's host (`HttpOnly`, `SameSite=Lax`) |
| 01f | Route switch in configuration, on and off | On in ~430 ms, rollback in ~220 ms, **same process** |
| 01g | Legacy system down | Friendly 503 page; new routes keep working |
| 01h | Cost of the facade | `Login.aspx` median: direct 2.84 ms, through the facade 3.31 ms (**+0.47 ms**) |

## What it decides

- The facade works **without touching the legacy system**, so it can be the first thing in production.
- Rolling back a screen is a configuration change, not a deploy.
- YARP forwards the destination `Host` by default, which IIS Express requires (with the original
  `Host` it answers 400).

## For production

- HTTPS terminates at the facade (secure cookies).
- The facade becomes the single entry point: two instances and monitoring.
- The legacy site must only be reachable through the facade (firewall or local binding), or the
  security headers added later can be bypassed.

Decision: [ADR-001](../../decisions/adr-001-facade.md).
