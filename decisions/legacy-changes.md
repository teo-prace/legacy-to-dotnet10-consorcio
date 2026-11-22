# Changes made to the legacy system

Project rule: **the legacy system changes as little as possible**, and every change is listed here with
the reason, the test that justifies it and how to undo it. If it isn't on this list, the migration
didn't touch it.

| # | What | Kind | Decision |
|---|---|---|---|
| 1 | `legacy/src/SGC.Web/Sessao/Quem.ashx` (new file) and the `PonteSessaoChave` setting in `web.config` | Addition, read-only | [ADR-002](adr-002-identity.md) |
| 2 | `legacy/database/procs/SP_BAIXA_PARCELA.sql`: lock the quota at the start of the transaction | **Behavior fix** (concurrency) | [ADR-006](adr-006-shared-database.md) |

New database objects owned by the new system live in their own schema (`novo`); no legacy table changes.

## 1 · Session bridge

- **What:** a handler that returns, as JSON, who is logged into the current session. Read-only
  (`IReadOnlySessionState`); requires the `X-SGC-Ponte` header with the key from `web.config` (without
  it: 404).
- **Why:** new screens need to know who is logged in without a second login.
- **Evidence:** spike 02 (6 tests).
- **Undo:** delete the file and the setting. Nothing in the legacy system depends on them.
- **Production:** the key comes from a secret store; the versioned value is for development only.

## 2 · Deadlock fix in `SP_BAIXA_PARCELA`

**The only behavior change in the legacy system so far.**

### The problem

Two payments **on the same contemplated quota**, different installments, at the same time:

```
payment A                                  payment B
UPDATE TB_PARCELA (installment 1)  🔒 P1   UPDATE TB_PARCELA (installment 2)  🔒 P2
UPDATE TB_COTA                     🔒 quota UPDATE TB_COTA  ⏳ waits for the quota (A has it)
settlement check: reads the
  quota's installments ⏳ waits for P2 (B has it)
                    → deadlock: SQL Server kills one of them (error 1205)
```

- **Measured:** 50 to 55 of 530 concurrent payments (~10%) died, over 5 rounds (test 05-4).
- **It happened before the migration:** the bank return batch and an operator posting payments on the
  same quota. The legacy screen doesn't retry and shows the deadlock message; the batch records the
  return line as rejected.

### The fix

The first statement inside the transaction locks the quota row:

```sql
BEGIN TRANSACTION

-- [MIGRACAO 2026-11 - inicio] trava a cota antes de tudo (ver cabecalho, ADR-006)
SELECT @LINHAS = COUNT(*) FROM TB_COTA WITH (UPDLOCK, HOLDLOCK) WHERE CD_COTA = @CD_COTA
-- [MIGRACAO 2026-11 - fim]
```

- Payments on the **same quota** queue up. Different quotas are not affected.
- **No business rule changed:** same values, postings, settlement and messages.
- Marked in the procedure header (history line `2026-11 MIGRACAO`) and between the
  `[MIGRACAO 2026-11 - inicio]` / `[MIGRACAO 2026-11 - fim]` comments. The comments are in Portuguese,
  like the rest of the client's code.

### Evidence

| Test | Procedure | Result |
|---|---|---|
| 05-4 | old version (the test removes the marked block on a database copy) | ~10% deadlocks without retry |
| 05-4b | fixed version (the one in the repository) | 530/530, **0 deadlocks**, no retry |
| 05-2, 05-3 | fixed | no double payment, same messages |

### Undo

Apply the previous version of the file (`git log -- legacy/database/procs/SP_BAIXA_PARCELA.sql`) with
`sqlcmd`. No data to migrate: the change is only about locking.
