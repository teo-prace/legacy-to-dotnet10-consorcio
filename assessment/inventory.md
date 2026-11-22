# Inventory

What the system is made of, measured from the source as received.

## Components

| Component | Technology | Since | Files | Lines | Runs on |
|---|---|---|---|---|---|
| Database `SGC` | SQL Server: 25 tables, 19 procedures, 6 functions, 1 trigger | 2005 | 32 | 3,481 | SQL Server (LocalDB in development) |
| Back office | ASP.NET WebForms, VB.NET, *Web Site project*, .NET Framework 4.8: 29 pages | 2011 | 66 | 4,216 | IIS |
| `SGC.Comum` | VB.NET library: data access, validation, formatting, log | 2011 | 9 | 382 | (referenced) |
| `SGC.Negocio` | VB.NET library: installment calculation, boleto | 2011 | 5 | 272 | (referenced) |
| `SGC.Batch` | VB.NET console, scheduled: billing run, bank return file, delinquency, yearly adjustment | 2016 | 9 | 451 | Windows Task Scheduler |
| Customer portal | Classic ASP + ADO: statement and boleto copy | 2007 | 10 | 405 | IIS (classic ASP), ODBC Driver 17 |
| `SGCBoleto.dll` | VB6 ActiveX DLL: boleto and late fees for the portal | 2007 | 5 | 276 | COM registration on the web server |
| Desktop | VB6 + ADO (`SQLOLEDB`): price indexes, draw verification | 2005 | 8 | 703 | Back-office workstations |

About 10,000 lines in total. Small enough to read entirely, which is what this assessment did; large
enough that nobody on the client side knows all of it.

## Dependencies

| Dependency | Used by | Note |
|---|---|---|
| .NET Framework 4.8 | back office, libraries, batch | Last version; security fixes only |
| IIS with classic ASP enabled | portal | Classic ASP must be installed as an IIS feature |
| COM registration (`regsvr32`) | portal → `SGCBoleto.dll` | A new server breaks the portal until someone remembers |
| `SQLOLEDB` provider | desktop | Deprecated by Microsoft |
| ODBC Driver 17 for SQL Server | portal | |
| VB6 IDE (or twinBASIC) | to rebuild the VB6 parts | VB6 IDE has been unsupported since 2008 |
| Fixed path `C:\SGC\Logs` | web, batch | Created on whatever machine runs the code |

## Integration points

| Point | Direction | Format |
|---|---|---|
| Bank return file | bank → batch | CNAB 400 (simplified), files dropped in a folder |
| Boleto | system → customer | FEBRABAN barcode and typeable line, bank 999 |
| Lottery results | operator → system | Typed in by hand on the assembly screen |

## Build and deploy

- No automated tests.
- Back office is a *Web Site project*: pages are compiled by IIS when first opened, so a compile
  error shows up only when someone opens the page.
- Deploy is a copy of the folder to the server.
- `Option Strict Off` in every project and `strict="false"` in `web.config`.
