# Spike 05 · Old and new screens writing to the same database

**Question:** during coexistence, legacy and new screens write to the **same database**. Is it safe? What
breaks when both post payments at the same time?

**Why it matters:** "shared database written from both sides" is the most cited failure of the
strangler fig pattern. The write rule gets duplicated, the copies drift apart and nobody knows which one
is right.

## Setup

- **The new screen writes only through the legacy procedure** (`SP_BAIXA_PARCELA`), with Dapper
  ([`BaixaParcelas.cs`](BaixaParcelas.cs)). No `UPDATE` on legacy tables from new code: the write rule
  (common fund, fee and reserve postings, late fees, excess, settlement, double-payment guard) stays in
  one place until the whole billing slice moves.
- What the new layer adds: business errors become results (`Recusada` / `Conflito`), not exceptions, and
  **a retry on deadlock** (error 1205; SQL Server has already rolled back the whole transaction, so
  retrying is safe).
- **The tests never touch the `SGC` database.** Each test restores a copy (`SGC_POC06`) from a backup
  taken at the start. The test with the legacy screen starts **a copy of the site** (port 8096) pointed
  at that copy.

Tests: `tests/Sgc.Tests/Spikes/Spike05SharedDatabaseTests.cs` (6/6).

## Result

| Test | Result |
|---|---|
| **05-1 Contract** | The procedure signature (8 parameters, names and types) and the columns read by the new grid are checked against the database. If someone changes the procedure, CI breaks before production does |
| **05-2 Race on one installment** (20 concurrent payments) | ✅ exactly **1** succeeds; 18 get "already paid" and 1 "changed by another process" (passed the read, lost at `UPDATE ... WHERE ID_SITUACAO = 2`). Postings and amortization once |
| **05-3 Operator on the legacy screen vs new screen** | ✅ both ways: the operator with a stale screen confirms and sees "Parcela ja paga."; the new screen gets `Conflito`. No double payment |
| **05-4 Different installments of the same contemplated quota, in parallel** | 🔴 **deadlock in the legacy procedure**: 50 of 530 payments (9.4%) were victims. With retry: 530/530 |
| **05-4b Same load, procedure fixed on the copy** | ✅ 530/530, **0 deadlocks, no retry** |
| **05-5 Grid read during a payment** | ⚠️ with the database as it is (`READ COMMITTED`), the grid query **waits for the payment transaction** (hit the 3 s test limit). With `READ_COMMITTED_SNAPSHOT`: 9 ms |

## The deadlock

Inside its transaction the procedure:

1. `UPDATE TB_PARCELA` (locks installment A);
2. `UPDATE TB_COTA` (locks the quota);
3. if the quota is contemplated, reads the quota's installments to check whether it is settled.

Two payments on the same quota (installments A and B) cross: the first holds the quota and, at step 3,
waits for installment B; the second holds B and waits for the quota. SQL Server kills one. **This
already happens in production today**, for instance when the bank return batch and an operator post
payments for the same quota; the legacy screen doesn't retry and shows the deadlock message.

**Fix tested on the copy (05-4b):** lock the quota first thing in the transaction
(`SELECT ... FROM TB_COTA WITH (UPDLOCK, HOLDLOCK) WHERE CD_COTA = @CD_COTA`). Payments on the same quota
queue up; different quotas are not affected. One statement in a legacy procedure, with this test as the
safety net.

Decision: [ADR-006](../../decisions/adr-006-shared-database.md).
