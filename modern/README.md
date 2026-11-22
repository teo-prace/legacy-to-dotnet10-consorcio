# SGC.Novo · the new system, growing next to the old one

Phase 3 of the [approach](../approach/README.md). One .NET 10 app that sits in front of the legacy site
and takes over screens one at a time, following the [migration plan](../decisions/migration-plan.md).

| Plan phase | What is here | State |
|---|---|---|
| 0 · Facade | YARP in front of the legacy site; anti-framing headers on every response; friendly 503 when the legacy site is down; the session bridge closed to the outside | done |
| 1 · Installment search | `Cobranca/Parcelas` in Blazor static SSR, serving the old URL, same rows as the legacy screen | done |
| 2 · Payment posting | `Cobranca/Baixa` in Blazor static SSR: fees from the single rule (half up), posted through `SP_BAIXA_PARCELA` with retry on deadlock, rounding recorded in the same transaction | done |
| 3 · Bank return batch and portal boleto | The billing rule and the boleto are already here and proven equal to the legacy code; the worker and the portal page are next | next |
| 4 · Billing 100% on .NET 10 | — | — |

## How it fits together

```
browser ──► SGC.Novo (.NET 10) ──┬── /novo/parcelas, /novo/baixa   new screens (Blazor static SSR)
              │                  │      └─ the old URLs land here when their switch is on
              │                  └── everything else ──► legacy site (WebForms, IIS)
              │
              ├─ who is logged in? ──► legacy Sessao/Quem.ashx (read-only bridge)
              └─ reads: SQL directly · writes: legacy procedures only
```

| Folder | Contents |
|---|---|
| [`src/Sgc.Novo/Program.cs`](src/Sgc.Novo/Program.cs) | The pipeline: security headers, switches, routing, the catch-all to the legacy site |
| [`src/Sgc.Novo/Identity`](src/Sgc.Novo/Identity) | Session bridge authentication (ADR-002) |
| [`src/Sgc.Novo/Billing`](src/Sgc.Novo/Billing) | Boleto, business calendar, late fees, payment posting (ADR-005, ADR-006) |
| [`src/Sgc.Novo/Data`](src/Sgc.Novo/Data) | Read queries on the legacy database |
| [`src/Sgc.Novo/Components`](src/Sgc.Novo/Components) | Blazor static SSR screens with the legacy layout and CSS |
| [`sql/`](sql) | Objects owned by the new system, in their own schema (`novo`) |
| [`Dockerfile`](Dockerfile) | Linux container image |

## Switches

Read on every request: changing `appsettings.json` or an environment variable switches without a deploy
or a restart. Turning a switch off is the rollback.

| Setting | Values | Effect |
|---|---|---|
| `Migracao:Parcelas` | `legado` (default) / `novo` | Who answers `/Cobranca/Parcelas.aspx` |
| `Migracao:Baixa` | `legado` (default) / `novo` | Who answers `/Cobranca/Baixa.aspx` |
| `Migracao:SeloTelaNova` | `true` / `false` | "TELA NOVA" badge on new screens: on in staging and during the pilot |

## Run it

With the legacy site running (`legacy\src\executar.cmd`, port 8090) and the `novo` schema created
(`sqlcmd -S "(localdb)\MSSQLLocalDB" -d SGC -i modern\sql\001_rounding_record.sql`):

```bat
dotnet run --project modern\src\Sgc.Novo -- --Migracao:Parcelas=novo --Migracao:Baixa=novo
```

Open http://localhost:8200, log in as `msouza` / `cecons123`, go to Cobrança → Parcelas and Baixa
manual. The menu is the legacy one; the screens are new.

Container:

```bat
docker build -t sgc-novo modern
```

## Tests

`tests/Sgc.Tests/Modern` (7 tests). The billing code is compiled into the test project as-is, so the parity
tests run the exact source that ships.

| Test | What it proves |
|---|---|
| M-1 | The new boleto equals the legacy VB.NET one on all 2,287 issued installments and on the due-date factor edges (0 differences) |
| M-2 | Late fees over 333,960 cases: **every** difference from the payment screen, the batch and the portal belongs to a category the business decided (rounding up; no fee within the grace day). Anything else fails |
| M-3 | Rounding record: written with the payment or not at all; costs +0.15 to 0.27 ms per payment and ~350 bytes per row |
| M-4 | Facade: legacy screens pass through, anti-framing headers on every response, the bridge returns 404 from outside, the Blazor script is served by the new side |
| M-5 | Legacy site down: the facade's own 503 page; new routes stay up |
| M-6 | With the switch on, the legacy menu leads to the new installment screen, which returns the same rows as the legacy one for five filters |
| M-7 | A payment posted on the new screen: fees shown equal the rule, posted through the legacy procedure (3 fund postings), rounding recorded; a read-only user can't post |

## For production

- HTTPS terminates here; the legacy site accepts connections only from this app.
- Two instances behind a load balancer; the app keeps no state of its own (session lives in the legacy
  system, data in SQL Server).
- The bridge key and the connection string come from a secret store.
- Logs carry `X-SGC-Origem` (`legado`, `novo`, `fachada`): "no billing request answered by the legacy
  system for 30 days" is the exit criterion of the slice.
