# How I modernize a legacy system

Most legacy migrations are sold as a rewrite: someone reads the old code, writes a new system, and
switches over on a weekend. That is the plan behind the failures everyone remembers. The old system
keeps years of rules nobody wrote down, the new one is "almost the same", and the differences show up
in production, in front of customers.

I work the other way around. The legacy system keeps running and keeps being the reference. The new
system grows next to it, one screen at a time, and nothing is switched on until a test proves it does
what the old one does.

This page is the shape of the method. The case in this repository applies it end to end to a
fictional consórcio administrator, CeCons, and every claim below points to the place where it was
done and measured.

## Three phases

| Phase | What the client gets | In this case |
|---|---|---|
| **1. Assessment** | What the system really does: inventory, risks, business rules and where they live, written down and backed by tests that record today's behavior | [`assessment/`](../assessment) |
| **2. Plan in small steps** | Decisions with evidence, and a sequence of slices where each one goes to production on its own and retires a piece of the old system | [`decisions/`](../decisions), [`spikes/`](../spikes) |
| **3. Migration with the system running** | New screens in production next to the old ones, behind switches that can be turned off in seconds | [`modern/`](../modern) |

## What is different from a conventional migration

| Conventional | This approach |
|---|---|
| Specify the new system from documents and interviews | **The legacy code is the oracle.** The old code is executed and the new code is compared with it, case by case. Documents describe what people think the system does; the code is what it does |
| Rewrite, then test the new system | **Parity before the switch.** Every migrated screen is compared with the legacy screen, running, before it is turned on |
| Big cutover | **One screen at a time, behind a facade.** Old and new share the same URL; each new screen has a switch, and going back is a configuration change, not a deploy |
| Fix the "wrong" behavior silently | **Divergences are measured and decided.** When the old system disagrees with itself, the business decides which rule wins, with numbers in hand |
| New and old both write to the database | **One owner per concern while they coexist.** Login stays with the legacy; writes go through the legacy's own procedures until the whole slice moves |
| Change the legacy whenever convenient | **Every change to the legacy is logged**, with the reason, the test that justifies it and how to undo it |
| "Phase 2" never ends | **Each phase retires something.** A plan that never turns the old system off is a plan to maintain two systems |
| Decisions live in meetings | **Decisions are written down with the evidence** (architecture decision records), so the next person knows why |

## Before any code: spikes

A migration plan is a list of bets: the facade will not break the old screens, the new screens will
know who is logged in, the database can take writes from both sides. Each bet gets a small, throwaway
experiment with automated tests before the plan depends on it. In this case there are six, and three of
them found real problems in the legacy system (a clickjacking exposure, a boleto bug that only appears
in 2050, and a deadlock in the payment procedure).

## About tooling

I use AI tools to read and write code faster. The decisions, the validation and the tests are mine,
and nothing is switched on without a test that compares it with the legacy system.

## What is not here

This is the shape of the method, not the whole playbook. Assessment templates, the risk checklist,
estimation and the rollout runbook are part of an engagement.
