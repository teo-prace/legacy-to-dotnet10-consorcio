# Business rules map

The rules the system actually applies, where each one lives in the code, and how many places implement
it. Nothing here comes from a specification: there was none. Each rule was read in the code and, for
the billing rules, confirmed by running the legacy code (see [characterization](README.md#characterization)).

**Implementations** counts independent copies of the same rule. Anything above 1 is a migration risk:
the new system has to pick one, and somebody's numbers will change.

## Plan and installments

| Rule | Where | Implementations |
|---|---|---|
| The obligation is a percentage of the asset value: 100% common fund + administration fee % + reserve fund %, all with 6 decimal places | `SP_GERA_PLANO_PARCELAS`, `SGC.Negocio/CalculoParcela.vb` | **2** (contract and simulation disagree in cents, finding R5) |
| Installment *k* of *n*: common fund `100/n`, fee `(TA − A)/n` plus the advance `A` on the first one, reserve `FR/n`; **the last installment absorbs the residue** | `SP_GERA_PLANO_PARCELAS` | 1 in the contract, the simulation skips the residue |
| Quota term = group term − assemblies already held (late joiners pay 100% in fewer installments) | `SP_EFETIVA_PROPOSTA` | 1 |
| The amount charged is the percentage × asset value **on the billing date** | `SP_EMITE_COBRANCA` | 1 |
| A quota excluded and resold becomes a new version (`015-1`); the old one still exists and is owed a refund | `TB_COTA`, `SP_EXCLUI_INADIMPLENTES`, `SP_DESISTE_COTA` | 1 |
| Regulation frozen at joining: the group copies the plan parameters | `SP_CONSTITUI_GRUPO` | 1, **contradicted by a trigger** (finding B1) |

## Billing

| Rule | Where | Implementations |
|---|---|---|
| Monthly billing: open installments due in the month, active or contemplated quotas, sequential "nosso número" with a modulo 11 check digit | `SP_EMITE_COBRANCA`, `FN_DV_MODULO11` | 1 |
| Boleto: FEBRABAN barcode and typeable line, bank 999, due-date factor | `SGC.Negocio/Boleto.vb`, `vb6/SGCBoleto/clsBoleto.cls` | **2** (factor differs from 2049 on, finding R6) |
| No late fees when paid up to the **next business day** after the due date (weekends and holidays in `TB_FERIADO`) | `FN_PROXIMO_DIA_UTIL`, payment screen, batch, portal | **4** (batch ignores holidays, portal ignores the grace period: R2, R3) |
| Late fees: fine = value × fine %, interest = value × monthly % / 30 × days since the due date | payment screen, batch, portal, load script | **4** (R1), with **4 rounding rules** (R4) |
| Posting a payment: rejects less than due; excess goes to the refund fund; principal split into common fund / fee / reserve as billed; late fees half common fund, half fee | `SP_BAIXA_PARCELA` | 1 |
| A contemplated quota with every installment paid becomes settled | `SP_BAIXA_PARCELA` | 1 |
| Bank return (CNAB 400): post each paid boleto, reject what the procedure refuses, keep the file history | `SGC.Batch/Retorno.vb` | 1 (with integrity problems: O1, O2) |

## Assembly and contemplation

| Rule | Where | Implementations |
|---|---|---|
| Draw by the Federal Lottery: base number = last three digits of the 1st prize (`000` = 1000), wrapped to the number of quotas; if that quota is not eligible, try prizes 2 to 5, then the next eligible quota above | `FN_NUMERO_SORTEADO`, `SP_APURA_ASSEMBLEIA` | **2** (the VB6 verification uses an older rule, R9) |
| Eligible quota: active, not contemplated, no overdue installment past the grace period | `FN_COTA_APTA` | 1 (slow, O7) |
| Contemplation only while the common fund covers the credit | `SP_APURA_ASSEMBLEIA` | 1 |
| Bids: free, fixed (groups that offer it) and embedded (part of the bid paid with the credit itself), registered until the day before the assembly; tie-break changed in 2015 | `SP_REGISTRA_LANCE`, `SP_APURA_ASSEMBLEIA` | 1 (B3) |
| A bid amortizes the common fund of the last open installments, from the last one backwards; fee and reserve keep their original months | `SP_AMORTIZA_LANCE` | 1 (B2) |
| Credit release after contemplation | `SP_LIBERA_CREDITO` | 1 |

## Quota life cycle

| Rule | Where | Implementations |
|---|---|---|
| Exclusion for delinquency (3 or more overdue installments) | `SP_EXCLUI_INADIMPLENTES`, `SGC.Batch` | 1 |
| Withdrawal | `SP_DESISTE_COTA` | 1 (almost a copy of exclusion, M5) |
| Refund to excluded and withdrawn quotas, by draw in the assembly (Lei 11.795, arts. 22 and 30) | `SP_APURA_ASSEMBLEIA`, `SP_LIBERA_CREDITO` | 1 (B6) |
| Yearly asset value adjustment by the asset's price index | `SP_REAJUSTA_BENS`, `SGC.Batch` | 1 (the group also has an index column that nobody reads) |
| Proposal withdrawal within the cooling-off period | `SP_ARREPENDIMENTO_PROPOSTA` | 1 (hard-coded 7 days, B8) |

## Where the rules live

| Layer | Rules found |
|---|---|
| Stored procedures and functions | Most of the contract, billing, assembly and life-cycle rules |
| WebForms code-behind | Late fees, simulation, customer registration, assembly validations |
| Batch | Late fees (its own copy), bank return |
| VB6 components | Boleto and late fees for the portal, draw verification |
| Trigger | Plan changes propagated to groups (B1) |
