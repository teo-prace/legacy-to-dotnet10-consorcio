# Spikes

A migration plan is a list of bets. Before the plan depends on one, it gets a small, throwaway
experiment with automated tests. Each spike answers **one question**; the answer feeds a decision in
[`decisions/`](../decisions).

| Spike | Question | Answer |
|---|---|---|
| [01 · Facade](01-facade) | Can a .NET 10 reverse proxy sit in front of the WebForms system without breaking anything? Can a screen be rolled back instantly? | Yes: 26/26 legacy screens work through it, +0.47 ms per request, rollback in ~220 ms without a restart |
| [02 · Identity](02-identity) | How does a new screen know who is logged into the legacy system? | A read-only session bridge (one file in the legacy system). Microsoft's System.Web adapters also work ([02a](02a-system-web-adapters)) but need 49 DLLs and changes to the login |
| [03 · iframe](03-iframe) | Should old and new screens coexist through iframes? | No. It works on the same origin with four mitigations, one problem has no clean fix, and cross-site is impossible. Found: the legacy system can be framed by any site today |
| [04 · UI](04-ui) | Razor Pages, Blazor static SSR or Blazor Interactive Server? | The same real screen built three times and compared row by row with the legacy one: Razor Pages and static SSR tie; Interactive Server loses refresh, deep links and state on deploy |
| [05 · Shared database](05-shared-database) | Is it safe for old and new screens to write to the same database at the same time? | No double payment under a 20-way race; found a deadlock in the legacy payment procedure (~10% of concurrent payments on the same quota) |

The spike code is lab code: written to answer the question and be thrown away, with comments in
Portuguese as they were written during the experiments. What survives goes into [`modern/`](../modern).

## Running

The tests are in [`tests/Sgc.Tests/Spikes`](../tests/Sgc.Tests/Spikes). They start what they need (the
legacy site on IIS Express, each spike on its own port) and use the Edge installed on the machine.

```bat
dotnet build spikes\01-facade
dotnet test tests\Sgc.Tests --filter Spike=01
```
