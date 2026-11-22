# How migrations like this one fail

Before deciding *how* to migrate SGC, the question was: how did similar migrations go wrong? Each lesson
below became a decision and a test.

## Cases

### TSB (UK, 2018): big bang without enough testing

The bank moved about 5 million customers to a new platform in a single weekend. Customers were locked
out for days, saw other people's accounts and had payments go wrong. The independent review pointed to
**insufficient testing** and a **go decision without evidence** that the platform was ready. The cost
went past £330 million, and regulators fined the bank £48.65 million in 2022.

- [The Register: TSB fined £48.65m](https://www.theregister.com/2022/12/20/tsb_fine/)
- [The Register: TSB's 2018 losses](https://www.theregister.com/2019/02/01/tsb_fy_2018_losses)

### Hertz vs Accenture (US, 2016–2019): a rewrite that never shipped

A new website and app were commissioned to replace the existing ones. Two years later nothing usable had
been delivered and Hertz sued for US$ 32 million. Delivery was "everything at the end": there was no
small piece in production proving the path.

- [The Register: Hertz sues Accenture](https://www.theregister.com/2019/04/23/hertz_accenture_lawsuit)

### Joel Spolsky, "Things You Should Never Do" (2000): rewriting from scratch

The essay about Netscape's rewrite: old, ugly code holds years of fixes for cases nobody documented.
Throwing it away throws that knowledge away. In this case, several of the legacy system's "problems" are
business rules (see [findings](../assessment/findings.md)).

- [Things You Should Never Do, Part I](https://joelonsoftware.com/2000/04/06/things-you-should-never-do-part-i)

### The strangler fig itself: how it fails

Replacing a system piece by piece behind a facade is what Microsoft, AWS and Thoughtworks recommend. Its
failures are well known too:

1. **It never finishes:** the new system grows, the old one is never turned off, and the company now
   maintains two.
2. **Shared database:** both sides write to the same tables; the write rule is duplicated and drifts.
3. **The facade becomes a bottleneck and a single point of failure.**
4. **Silent divergence:** the new screen "works" but calculates differently, and nobody compares.

- [Microsoft: Strangler Fig pattern](https://learn.microsoft.com/azure/architecture/patterns/strangler-fig)
- [Martin Fowler: Strangler Fig Application](https://martinfowler.com/bliki/StranglerFigApplication.html)
- [AWS: strangler fig](https://docs.aws.amazon.com/prescriptive-guidance/latest/cloud-design-patterns/strangler-fig.html)
- [Thoughtworks: embracing the strangler fig pattern](https://thoughtworks.com/insights/articles/embracing-strangler-fig-pattern-legacy-modernization-part-one)

### Technical references used in the spikes

- [Microsoft: incremental ASP.NET to ASP.NET Core migration](https://learn.microsoft.com/aspnet/core/migration/inc/start)
  and [remote session](https://learn.microsoft.com/aspnet/core/migration/inc/remote-session) (spike 02a)
- [Blazor and SignalR](https://learn.microsoft.com/aspnet/core/blazor/fundamentals/signalr) (spike 04)
- [web.dev: authentication inside iframes](https://web.dev/articles/webauthn-within-iframe) (spike 03)

## Lesson → decision → evidence

| Lesson | Decision here | Evidence |
|---|---|---|
| Big bang with no way back (TSB) | One screen at a time behind a switch; rolling back is a configuration change | Spike 01: rollback in ~220 ms, no restart |
| Going live without evidence (TSB) | No screen is switched on without a **parity test** against the legacy screen, running | Spike 04 (caught a filter bug), characterization C-2 (333,960 cases) |
| A rewrite that never ships (Hertz) | The first production delivery is **the facade alone**, which already fixes a security exposure | Spikes 01 and 03 |
| Old code holds the rules (Spolsky) | The legacy code is the **oracle**: executed and compared, not re-specified | Characterization: found the four late-fee rules and a VB6 bug in 2050 |
| Silent divergence | Divergences are measured, categorized and decided by the business | Characterization → ADR-005 |
| Shared database written from both sides | While they coexist, new code writes **only through the legacy procedures**; contract test in CI | Spike 05 (found a deadlock in the legacy system) |
| The facade as a single point of failure | Stateless facade, its own 503 page, two instances in production | Spike 01 (503 with the legacy system down) |
| It never finishes | Every phase of the plan says which legacy parts **are turned off** | [migration plan](migration-plan.md) |
| Duplicated identity | One login (the legacy one) until the end; the new side only reads it | Spike 02 |
| Coexisting through iframes | Don't; the facade already gives the same origin with full pages | Spike 03 (9 scenarios) |
