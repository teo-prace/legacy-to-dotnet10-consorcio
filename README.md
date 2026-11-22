# Modernizing a legacy consórcio system without stopping it

A complete, runnable case of legacy modernization: a 20-year-old Microsoft stack (VB6, classic ASP,
ASP.NET WebForms, VB.NET, SQL Server stored procedures) moved to .NET 10 one slice at a time, while the
old system keeps running.

> **Fictional company, realistic system.** CeCons Administradora de Consórcios does not exist. The
> system was built to demonstrate the method on something with the weight of a real one: Brazilian
> consórcio rules (Lei 11.795/2008, BCB Resolution 285/2023), 25 tables, 26 procedures, a batch,
> a customer portal, COM components, and the problems that accumulate in two decades of maintenance.
> The legacy code and its data are in Portuguese, as they would be at a Brazilian client.

## The approach

Assessment first, then a plan in small steps, then migration with the system running. The legacy code
is the oracle, every migrated screen is compared with the old one before it is switched on, and each
phase retires part of the old system. Details in [approach](approach/README.md).

## Results so far

| | |
|---|---|
| Legacy screens working through the new .NET 10 facade | 26/26, +0.47 ms per request |
| Rolling a screen back to the legacy system | ~220 ms, configuration only, no restart |
| Late-fee cases compared with the legacy code, executed | 333,960; every difference belongs to a category the business decided |
| New boleto vs the legacy VB.NET one | 0 differences over 2,287 issued installments |
| New installment screen vs the legacy one | same rows, same order, five filters |
| Problems found in the legacy system along the way | a clickjacking exposure (fixed by the facade), a deadlock in the payment procedure (fixed, logged), late fees charged to customers who paid on time (decided, fixed when each part migrates), a boleto bug due in 2050 |
| Bug caught in the new screen before it shipped | 1, by the parity test (a filter that silently returned 12 rows instead of 50) |
| Automated tests | 48 (characterization, spikes, new system) |

## Repository map

| Folder | What it is | Status |
|---|---|---|
| [`legacy/`](legacy) | The client's system as received: WebForms back office, classic ASP portal, VB6 components, batch, SQL Server | — |
| [`approach/`](approach) | How the migration is run and what makes it different | done |
| [`assessment/`](assessment) | Phase 1: inventory, business rules map, 36 findings, characterization tests | done |
| [`decisions/`](decisions) | Phase 2: research on failed migrations, six architecture decision records, migration plan, changes to the legacy system | done |
| [`spikes/`](spikes) | Five experiments with automated tests behind the decisions | done |
| [`modern/`](modern) | Phase 3: SGC.Novo, the .NET 10 system growing next to the legacy one (facade, installment search, payment posting, container image) | plan phases 0 to 2 done |
| [`tests/`](tests/Sgc.Tests) | One test project: `Area=Characterization`, `Spike=01..05`, `Area=Modern` | — |
| [`tools/oracle`](tools/oracle) | Runs the legacy VB.NET code on .NET Framework 4.8 so tests can compare against it | — |

## The legacy system

| Part | Technology | Since |
|---|---|---|
| Database | SQL Server: 25 tables, 26 procedures, functions and a trigger | 2005 |
| Desktop | VB6 + ADO | 2005 |
| Customer portal | Classic ASP + ADO | 2007 |
| Boleto component | VB6 ActiveX DLL | 2007 |
| Back office | ASP.NET WebForms, VB.NET, .NET Framework 4.8 | 2011 |
| Batch | VB.NET console | 2016 |

## Running it

Windows with Visual Studio (ASP.NET and .NET Framework 4.8), SQL Server LocalDB, IIS Express, the .NET 10
SDK, Edge, and [twinBASIC](https://twinbasic.com) for the VB6 component.

```bat
legacy\database\criar_banco.cmd                       :: database with sample data
legacy\src\build.cmd                                  :: legacy system
powershell legacy\vb6\SGCBoleto\compilar.ps1          :: VB6 component (build and register)
dotnet build tools\oracle                             :: legacy oracle
sqlcmd -S "(localdb)\MSSQLLocalDB" -d SGC -i modern\sql\001_rounding_record.sql

dotnet test tests\Sgc.Tests                           :: starts what it needs (IIS Express, apps)
```

To see it: [legacy/README.md](legacy/README.md) for the old system, [modern/README.md](modern/README.md)
for the new one. CI builds everything and the container image; the full test suite runs on a development
machine.
