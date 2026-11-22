# Decisions

Phase 2. Every decision below came out of a spike with automated tests or out of the characterization of
the legacy system. Nothing was decided on opinion alone.

1. [How migrations like this one fail](failed-migrations.md): TSB, Hertz, Spolsky and the known failures
   of the strangler fig, and the decision each lesson produced.
2. Architecture decision records:

| ADR | Decision |
|---|---|
| [001](adr-001-facade.md) | .NET 10 + YARP facade in front of the legacy system, a switch per screen, rollback without deploy |
| [002](adr-002-identity.md) | Identity through a read-only session bridge (one file in the legacy system) |
| [003](adr-003-no-iframes.md) | No iframes for coexistence; anti-framing headers in phase 0 |
| [004](adr-004-ui.md) | UI: Blazor static SSR; Interactive Server only as an exception |
| [005](adr-005-billing-rules.md) | One billing rule in C#, proven against the legacy code; half a cent rounds up, every case recorded |
| [006](adr-006-shared-database.md) | Shared database: writes only through the legacy procedures; deadlock fixed in the procedure |

3. [Migration plan for billing](migration-plan.md): five phases, what goes in and what is turned off in
   each one.
4. [Changes made to the legacy system](legacy-changes.md): the complete list, with reason, evidence and
   how to undo.
