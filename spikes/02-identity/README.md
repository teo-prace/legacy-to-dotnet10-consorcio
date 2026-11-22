# Spike 02 · Who is logged in?

**Question:** a new .NET 10 screen sits behind the facade. How does it know who is logged into the
legacy system, which keeps the user as a `DataRow` in an in-process session (`Session("Usuario")`), with
no Forms Authentication?

Two approaches, both built and tested:

| | 02a · Microsoft System.Web adapters (remote session) | 02 · Session bridge |
|---|---|---|
| Folder | [`02a-system-web-adapters`](../02a-system-web-adapters) | this one |
| Change to the legacy system | 49 DLLs in `Bin`; `web.config` module, key, `netstandard` reference and 8 binding redirects; registration in `Global.asax`; **login rewritten to store serializable keys** ([script](../02a-system-web-adapters/preparar-legado.ps1)) | **One file** (`Sessao/Quem.ashx`, ~60 lines) and one setting |
| Reads the current `DataRow` session | No | Yes |
| Writes to the legacy session | Yes | No (read-only on purpose) |
| Cost per request (median, local) | 1.3 ms | 0.2 ms with a 5 s cache / 3.0 ms without |

## How the bridge works

The new app forwards the legacy session cookie to `Sessao/Quem.ashx`, which answers
`{codigo, login, nome, perfil, expiraEmMinutos}` for the current session. An ASP.NET Core
`AuthenticationHandler` turns that into a `ClaimsPrincipal`; legacy profiles become policies.

- The bridge requires a shared key header (`X-SGC-Ponte`, constant-time comparison) and the facade
  answers 404 for `/Sessao/*` from outside.
- Only positive answers are cached (5 s, configurable).
- Login, logout and timeout stay with the legacy system.

## Result (6/6)

| # | Scenario | Result |
|---|---|---|
| 1 | New screen, no session | Goes to the legacy login and **comes back to the new screen** logged in |
| 2 | Legacy profile → authorization | Operator gets 403 in the admin area; admin gets 200 |
| 3 | Logout in the legacy system | The new screen asks for login within 5.4 s (the cache time) |
| 4 | Bridge called from outside | 404 through the facade even with the key; 404 directly without it |
| 5 | Legacy screens forwarded | **0** bridge calls (authentication runs only on new routes) |
| 6 | 80 s on new screens only, legacy session timeout 1 min | The legacy session **stays alive** (the bridge renews it) |

## Problems the tests found

1. **Automatic default scheme.** Since ASP.NET Core 7, a single registered scheme becomes the default
   and runs on **every** request, including the ones forwarded to the legacy system. Turned off with
   `Microsoft.AspNetCore.Authentication.SuppressAutoDefaultScheme`.
2. **Caching "nobody logged in" breaks login.** The legacy cookie exists before login (`Session_Start`),
   so a cached negative answer sent the user back to the login page in a loop. Only positive answers
   are cached.
3. **IIS Express hangs** when its redirected output isn't read (one line per request fills the pipe).
4. **`CookieContainer` with an explicit `Domain`** sends RFC 2965 cookies (`$Version=1`) and the session
   id arrives wrong.

Decision: [ADR-002](../../decisions/adr-002-identity.md).
