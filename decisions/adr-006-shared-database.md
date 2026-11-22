# ADR-006 · Shared database while old and new coexist

**Status:** accepted · **Evidence:** [spike 05](../spikes/05-shared-database) (6 tests)

## Context

Legacy and new screens use the same SQL Server database. A shared database written from both sides is the
most cited failure of the strangler fig ([research](failed-migrations.md)): the write rule gets
duplicated and drifts.

## Decision

1. **Writes only through the legacy procedures while both coexist.** New code doesn't
   `INSERT`/`UPDATE`/`DELETE` legacy tables. The new payment screen calls `SP_BAIXA_PARCELA`, which keeps
   owning the fund postings, late fees, excess, settlement and the double-payment guard.
2. **Contract test in CI:** signature of the procedures used and columns read by new screens.
3. **Retry on deadlock (error 1205) in the new layer**, with growing back-off. Business errors
   (`RAISERROR`) become results; "already paid" becomes `Conflict` and is shown to the user.
4. **Fix the deadlock in `SP_BAIXA_PARCELA`** by locking the quota first thing in the transaction (one
   statement). It also protects the legacy screen and the batch, which don't retry. Logged in
   [legacy-changes.md](legacy-changes.md).
5. **Do not turn on `READ_COMMITTED_SNAPSHOT` now.** It removes the wait of reads during a payment
   (3 s+ → 9 ms), but changes the behavior of every read in the legacy system and needs its own spike over
   the whole system.

## What the spike measured

| Scenario | Result |
|---|---|
| 20 concurrent payments of one installment | 1 posted, 19 conflicts; postings once |
| Operator with a stale legacy screen vs new screen | No double payment either way; the second one is warned |
| Concurrent payments of different installments of one contemplated quota (old procedure) | **Deadlock in ~10%** (50 to 55 of 530) — already happening in the legacy system |
| Same, with retry | 530/530 |
| Same, procedure fixed, no retry | 530/530, 0 deadlocks |
| Grid read during a payment | waits for the transaction; with RCSI it doesn't |

## Consequences

- ✅ The write rule stays in one place until the whole billing slice migrates; then it moves at once,
  with these tests as parity.
- ⚠️ New code depends on procedures the legacy team can change: the contract test is the protection.
- ⚠️ The deadlock fix is the first behavior change the migration makes in the legacy system. It comes with
  the test that reproduces the problem and the one that proves the fix.
