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

## Repository map

| Folder | What it is | Status |
|---|---|---|
| [`legacy/`](legacy) | The client's system as received: WebForms back office, classic ASP portal, VB6 components, batch, SQL Server | done |
| [`approach/`](approach) | How the migration is run and what makes it different | done |
| [`assessment/`](assessment) | Phase 1: inventory, risks, business rules map, characterization tests | in progress |
| [`decisions/`](decisions) | Phase 2: research on failed migrations, architecture decision records, migration plan | in progress |
| [`spikes/`](spikes) | Experiments with automated tests behind each decision | in progress |
| [`modern/`](modern) | Phase 3: the .NET 10 system growing next to the legacy one | in progress |

## The legacy system

| Part | Technology | Since |
|---|---|---|
| Database | SQL Server: 25 tables, 26 procedures, functions and a trigger | 2005 |
| Desktop | VB6 + ADO | 2005 |
| Customer portal | Classic ASP + ADO | 2007 |
| Boleto component | VB6 ActiveX DLL | 2007 |
| Back office | ASP.NET WebForms, VB.NET, .NET Framework 4.8 | 2011 |
| Batch | VB.NET console | 2016 |

To run it locally, see [legacy/README.md](legacy/README.md) (Windows, SQL Server LocalDB, IIS Express).
