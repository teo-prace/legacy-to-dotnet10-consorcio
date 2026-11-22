# Migration plan: billing (*Cobrança*) to .NET 10

The first slice. Every phase ships something to production, has entry and exit criteria, and says
**what part of the legacy system is turned off**: a strangler fig that never turns anything off is the
pattern's most common failure ([research](failed-migrations.md)).

## Principles (every phase)

1. **One screen at a time, behind a switch.** Going back to the legacy screen is a configuration change
   (ADR-001).
2. **Parity before the switch.** The new screen is compared with the legacy one, running; divergences are
   measured and decided, never "fixed" silently (ADR-004, ADR-005).
3. **The legacy system changes as little as possible**, and every change comes with the test that
   justifies it ([legacy-changes.md](legacy-changes.md)).
4. **One source of truth per concern:** login in the legacy system (ADR-002); writes through the legacy
   procedures until the whole slice moves (ADR-006).

## Phase 0 · The facade in production (immediate win, no new screen)

| | |
|---|---|
| Delivers | .NET 10 + YARP facade in front of SGC, forwarding everything. Anti-framing headers (ADR-003). HTTPS at the facade |
| Why first | Fixes today's clickjacking exposure and proves the infrastructure with almost no risk: if anything goes wrong, DNS / binding points back to IIS |
| Production | Two facade instances; the legacy IIS only accepts connections from the facade; logs with `X-SGC-Origem`; alerts on 5xx and latency |
| Exit criteria | Spike 01 tests green against staging; added latency < 5 ms (p95); one week without incidents |
| Turned off | — |

## Phase 1 · Installment search on the new screen (read only)

| | |
|---|---|
| Delivers | Session bridge (`Sessao/Quem.ashx`) + `Cobranca/Parcelas` in Blazor SSR (ADR-004), serving the old URL |
| Before the switch | Parity test with the 5 filters against the legacy screen; contract test of the columns |
| Switch | On for a group of operators for a week, then for everyone |
| Exit criteria | Parity green; no divergence tickets; rollback rehearsed once in staging |
| Turned off | `Parcelas.aspx` (the file stays until phase 4, with no route) |

## Phase 2 · Payment posting on the new screen

| | |
|---|---|
| Delivers | New `Cobranca/Baixa` in Blazor SSR: fees from the single C# rule (half up, ADR-005), posting through `SP_BAIXA_PARCELA` with retry on deadlock, rounding record |
| Before the switch | Deadlock fix applied in production (test 05-4b against staging); characterization and concurrency tests green; **finance team informed**: the new screen may charge R$ 0.01 more than the old one in fine or interest |
| Turned off | `Baixa.aspx` |

## Phase 3 · Bank return batch and boleto copy in the portal

| | |
|---|---|
| Delivers | Bank return as a .NET 10 worker with the same rule; boleto copy as a new page, replacing classic ASP + VB6 |
| Business effect | No more undue fees from the batch (holidays) and the portal (no grace day): **finance and customer service informed** (ADR-005) |
| Before the switch | Return files processed in parallel (legacy posts, new only compares) for a full monthly cycle, with zero differences outside the decided categories |
| Turned off | `SGC.Batch` (return), `SGCBoleto.dll` (VB6), the portal's boleto pages |

## Phase 4 · Billing 100% on .NET 10

| | |
|---|---|
| Delivers | The payment write rule moves from the procedure to C#, in one transaction, with the spike 05 tests as parity |
| Turned off | Billing screens, procedures and VB code. Legacy billing routes removed from the facade |
| Done when | No billing request answered with `X-SGC-Origem: legado` for 30 days |

## Out of scope for this slice

Assemblies, contemplation, proposals and registration stay on the legacy system, behind the facade. The
next slice is chosen after phase 2, with what this one teaches.

## Decisions

| # | Decision | Status |
|---|---|---|
| 1 | UI: Blazor static SSR (ADR-004) | decided |
| 2 | Rounding: half up, every case recorded (ADR-005) | decided |
| 3 | Fix the deadlock in `SP_BAIXA_PARCELA` (ADR-006) | decided; in the repository; production in phase 2 |
| 4 | `READ_COMMITTED_SNAPSHOT` (ADR-006) | open, blocks nothing; recommendation: not now, own spike first |
