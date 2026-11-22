# ADR-002 · Identity through a read-only session bridge

**Status:** accepted · **Evidence:** [spike 02](../spikes/02-identity) (6 tests) and [spike 02a](../spikes/02a-system-web-adapters) (2 tests)

## Context

The legacy login stores a `DataRow` in `Session("Usuario")` (in-process session, no Forms
Authentication). New screens need to know who is logged in and with which profile, without a second
login.

## Decision

**Session bridge:** a handler in the legacy system (`Sessao/Quem.ashx`, ~60 lines, read-only) returns
`{codigo, login, nome, perfil, expiraEmMinutos}` for the current session. On .NET 10, an
`AuthenticationHandler` forwards the legacy cookie to the bridge and builds the `ClaimsPrincipal`.

- The bridge requires a shared key (`X-SGC-Ponte`, constant-time comparison) and is not reachable from
  outside: the facade answers 404 for `/Sessao/*`.
- Only positive answers are cached, for 5 s (configurable).
- Login, logout and timeout stay with the legacy system. New screens **never write** to the session.
- Authentication runs only on new routes (`SuppressAutoDefaultScheme`).

## Alternatives

| | Microsoft System.Web adapters (remote session) | Session bridge |
|---|---|---|
| Change to the legacy system | 49 DLLs, `web.config` (module, key, `netstandard`, 8 redirects), `Global.asax`, **login rewritten** | 1 file and 1 setting |
| Reads the existing `DataRow` | No (needs serializable keys) | Yes |
| Writes to the session | Yes | No |
| Cost per request (median) | 1.3 ms | 0.2 ms cached; 3.0 ms not |

The adapters work and stay documented as **plan B**, in case a new screen ever needs to write to the
legacy session.

## Consequences

- ✅ A logout in the legacy system applies to new screens within 5 s; the legacy session stays alive
  while the user works only on new screens.
- ✅ Legacy profiles become .NET policies.
- ⚠️ In production the key comes from a secret store, not from the versioned `web.config`.
- ⚠️ When the login itself migrates, the bridge is the last thing to go.
- Logged as a change to the legacy system: [legacy-changes.md](legacy-changes.md).
