# ADR-005 · One billing rule (boleto, fines and interest)

**Status:** accepted · **Evidence:** [characterization tests](../assessment/README.md#characterization) (C-1 to C-3)

## Context

The legacy system calculates late fees in four places with four different rules. The characterization
tests executed the legacy code itself (VB.NET through a .NET Framework 4.8 harness, VB6 through COM, the
payment screen through the browser) over 333,960 cases.

| Where | Fee-free until | Holidays | Rounding |
|---|---|---|---|
| Payment screen (WebForms) | next business day | yes | banker's |
| Bank return batch (VB.NET) | next business day | **no** | half away from zero |
| Portal (VB6) | **no grace day** | — | banker's |
| SQL (`ROUND`) | — | — | half away from zero |

## Decision

1. **One C# implementation** of the boleto and of the late fees, used by the back office, the batch and
   the portal as each one migrates.
2. **Fee-free until the next business day, holidays included (`TB_FERIADO`).** It is the payment
   screen's rule and the regulation's. The batch (1,485 cases in the matrix) and the portal (4,356 cases)
   charge customers who paid on time; when they migrate, they stop. **This is a correction in the
   customer's favor and has to be announced to the finance team**, not discovered by them.
3. **Boleto:** the cyclic due-date factor (as in VB.NET; 0 differences over 2,287 installments). The VB6
   component produces invalid boletos from 2050 on and is not fixed: it goes away with the portal.
4. **Rounding: half a cent rounds up** (half away from zero), like SQL Server, the batch and the bank
   return file, so SQL reports match the screens. The payment screen used banker's rounding; the
   difference is at most R$ 0.01 in the fine and in the interest (11 of 42 cases checked on the screen
   change).
5. **Every rounding is recorded.** Each payment posted by the new system with fees writes the exact value
   before rounding, the value charged and what the old (banker's) rule would have charged, in the same
   transaction as the payment. "How much did this decision change, and for whom?" gets answered with
   data.

## Consequences

- ✅ Parity proven by tests that keep running in CI while the legacy system exists.
- ✅ The out-of-process oracle works for any legacy assembly that .NET 10 can't load.
- ⚠️ While they coexist, the batch and the portal keep their old rules: the divergence exists today and
  lasts until each one migrates. The plan prioritizes the bank return batch for that reason.
