# Findings

Everything below was found by reading the code and running it. Severity is about the business, not
about code style: **High** means money, customer data or legal exposure; **Medium** means a recurring
operational cost or a trap for the next change; **Low** means maintenance friction.

Paths are relative to [`legacy/`](../legacy); back-office pages (`Cobranca/...`, `Cadastro/...`) are under `legacy/src/SGC.Web`, procedures and functions under `legacy/database/procs`.

## 1. The same rule, implemented several times, disagreeing

The most expensive kind of finding in a migration: whichever implementation the new system copies, it
will be "wrong" for somebody.

| # | Finding | Where | Severity |
|---|---|---|---|
| R1 | **Late fees are calculated in four places** (back-office payment screen, bank return batch, portal component, data load script) with different rules. The batch hard-codes 2% and 1% instead of reading the parameter table | `src/SGC.Web/Cobranca/Baixa.aspx.vb`, `src/SGC.Batch/Encargos.vb`, `vb6/SGCBoleto/clsEncargos.cls` | High |
| R2 | **The batch ignores holidays.** A boleto due on a weekday holiday and paid the next business day is charged a fine by the batch, and the bank return is rejected as "paid less than due". The back office accepts the same payment | `src/SGC.Batch/Encargos.vb` | High |
| R3 | **The portal ignores the next-business-day grace.** A customer who pays on Monday a boleto due on Sunday is shown a fine in the boleto copy; the back office and the bank don't charge it | `vb6/SGCBoleto/clsEncargos.cls` | High |
| R4 | **Four rounding rules**: banker's rounding on the payment screen and in VB6, half away from zero in the batch and in T-SQL. Differences of R$ 0.01 | same as R1 | Medium |
| R5 | **Installment calculated twice**: the stored procedure (contract) and the VB.NET simulation. The simulation does not adjust the last installment, so simulated and contracted totals differ by cents (the simulation screen shows 100.000020% of common fund) | `database/procs/SP_GERA_PLANO_PARCELAS.sql`, `src/SGC.Negocio/CalculoParcela.vb` | Medium |
| R6 | **Boleto due-date factor fixed two ways.** VB6 subtracts 9000 days after 22/02/2025 (an emergency fix); .NET uses the cyclic formula. Same result today; VB6 produces invalid boletos when the factor resets again in 2049 | `vb6/SGCBoleto/clsBoleto.cls`, `src/SGC.Negocio/Boleto.vb` | Low (today) |
| R7 | **CPF validated in three places**; the JavaScript one accepts `111.111.111-11` | `src/SGC.Web/js/sgc.js`, `src/SGC.Comum/Validacao.vb`, `FN_CPF_VALIDO` | Low |
| R8 | **Two ways to register a customer**: the proposal procedure (validates, upper-cases the name) and a direct `INSERT` from the customer screen | `Comercial/PropostaEdicao.aspx.vb`, `Cadastro/ConsorciadoEdicao.aspx.vb` | Medium |
| R9 | **Draw verification in the VB6 desktop uses an old rule** (ignores delinquency, does not try lottery prizes 2 to 5) and flags 7 of 29 past assemblies as divergent. The back office has learned to ignore it | `vb6/SGCDesktop/clsSorteio.cls` | Medium |

## 2. Hidden or undocumented business behavior

| # | Finding | Where | Severity |
|---|---|---|---|
| B1 | **A trigger propagates plan changes to groups in formation**, against the "frozen regulation" principle for people who already joined | `database/procs/TR_PLANO_ATUALIZA_GRUPO.sql` | High |
| B2 | Bids amortize **only the common fund**; at the end of the term the quota pays installments made of fees only. Nobody remembers why | `SP_AMORTIZA_LANCE` | Medium |
| B3 | The bid tie-break rule changed in 2015; only a comment in the procedure records it, and old assemblies were decided by the previous rule | `SP_APURA_ASSEMBLEIA` | Medium |
| B4 | An assembly can be run before its date: the screen warns, the procedure doesn't check, and contemplations get a future date | `Assembleia/AssembleiaEdicao.aspx.vb`, `SP_APURA_ASSEMBLEIA` | Medium |
| B5 | Blank lottery prizes become `00000`, and `000` counts as 1000: in an 80-quota group the draw points to quota 040 every time | `AssembleiaEdicao.aspx.vb`, `SP_APURA_ASSEMBLEIA` | Medium |
| B6 | Refunds to excluded quotas come straight from the common fund and do not return the reserve fund paid | `SP_APURA_ASSEMBLEIA`, `SP_LIBERA_CREDITO` | Medium |
| B7 | Price indexes can only be maintained in the VB6 desktop; the web only reads them | `vb6/SGCDesktop`, `Cadastro/Indices.aspx` | Medium |
| B8 | `QT_DIAS_ARREPENDIMENTO` exists in the parameter table; the procedure uses 7 hard-coded | `SP_ARREPENDIMENTO_PROPOSTA` | Low |
| B9 | Contemplated delinquent quotas are never handled by the system; the batch lists them in a log for a team that works on spreadsheets | `SP_EXCLUI_INADIMPLENTES`, `SGC.Batch` | Medium |

## 3. Security

| # | Finding | Where | Severity |
|---|---|---|---|
| S1 | **SQL injection**: customer search, bank return file reader, lottery prize update, portal login, VB6 desktop login | `Cadastro/Consorciados.aspx.vb`, `SGC.Batch/Retorno.vb`, `AssembleiaEdicao.aspx.vb`, `asp/portal/default.asp`, `vb6/SGCDesktop/frmLogin.frm` | High |
| S2 | **Portal does not check ownership**: changing `cota=` or `parcela=` in the URL shows another customer's statement or boleto | `asp/portal/extrato.asp`, `segundavia.asp` | High |
| S3 | A back-office page opens **without login** (it inherits `Page` instead of the base page that checks the session) | `Grupos/Extrato.aspx.vb` | High |
| S4 | Passwords hashed with **unsalted MD5**; the session keeps the whole user row, hash included | `SGC.Comum/Criptografia.vb`, `App_Code/PaginaBase.vb` | High |
| S5 | **Open redirect** after login (`//other-site` passes the check) | `Login.aspx.vb` | Medium |
| S6 | Credentials in comments: `sa` connection string in `web.config`, portal user password in `conexao.asp` | `src/SGC.Web/web.config`, `asp/portal/include/conexao.asp` | High |

## 4. Data integrity and operations

| # | Finding | Where | Severity |
|---|---|---|---|
| O1 | Bank return: the detail row is saved as "paid" before the payment is posted, then **deleted and rewritten** as rejected if the procedure refuses, with no transaction between the two | `SGC.Batch/Retorno.vb` | High |
| O2 | An already-processed return file makes the batch fail and **stays in the folder**, failing again every night | `SGC.Batch/Retorno.vb` | Medium |
| O3 | Proposal number is `COUNT(*) + 1`: two simultaneous proposals collide and the user sees "Error saving" | `Comercial/PropostaEdicao.aspx.vb` | Medium |
| O4 | Errors swallowed by `Try/Catch` that show "Error saving"; the cause is lost | code-behind | Medium |
| O5 | Logs go to a fixed `C:\SGC\Logs`; the `web.config` setting `PastaLog` is never read | `SGC.Comum/Log.vb` | Low |
| O6 | Asset value stored in two places, kept in sync by the screen | `TB_BEM`, `TB_BEM_VALOR` | Low |
| O7 | Eligibility function calls another scalar function in a loop, per installment, per quota, per grid row: fine with 80 quotas, minutes with 3,000 | `FN_COTA_APTA` | Medium |

## 5. Maintainability

| # | Finding | Where | Severity |
|---|---|---|---|
| M1 | No automated tests; deploy by copying a folder | — | High |
| M2 | Status codes as magic numbers in VB and T-SQL, although a domain table and an `Enum` exist (used in half the places) | several | Low |
| M3 | SQL Server 2000 style procedures (`@@ERROR` + `GOTO`, cursors) | `database/procs` | Low |
| M4 | `Option Strict Off` everywhere | `*.vbproj`, `web.config` | Low |
| M5 | Exclusion and withdrawal do almost the same thing in two procedures | `SP_EXCLUI_INADIMPLENTES`, `SP_DESISTE_COTA` | Low |
