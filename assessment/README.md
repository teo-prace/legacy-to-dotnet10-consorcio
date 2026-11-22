# Assessment

Phase 1. Before planning anything, find out what the system really does, what it depends on and where
it can hurt the business. This is the report the client receives at the end of the phase.

| Document | Contents |
|---|---|
| [inventory.md](inventory.md) | Components, size, dependencies, integration points, build and deploy |
| [business-rules.md](business-rules.md) | Every business rule found, where it lives and how many copies of it exist |
| [findings.md](findings.md) | 36 findings with severity: divergent rules, hidden behavior, security, integrity, maintainability |

## Summary

SGC is about 10,000 lines spread over six technologies and twenty years: SQL Server procedures from
2005, a VB6 desktop, a classic ASP portal with a VB6 COM component, a WebForms back office from 2011 and
a VB.NET batch from 2016. It works. The business runs on it every day. Nobody on the client side knows
all of it, and there are no tests.

**What matters most for a migration:**

1. **The same rule lives in several places and the copies disagree.** Late fees are calculated in four
   places with four different results in edge cases. The bank return batch charges a fine to customers
   who paid on time after a holiday, and the portal does it after a weekend. A migration that copies
   "the" rule from any one place changes somebody's numbers without anyone deciding it.
2. **Business behavior is hidden in places nobody looks**: a trigger that changes the terms of groups
   already sold, a bid rule nobody can explain, a tie-break that changed in 2015 with no record but a
   comment.
3. **Security exposures that don't wait for a migration**: SQL injection in five places, a portal where
   any customer can read another customer's statement by changing the URL, a back-office page that opens
   without login, unsalted MD5 passwords.
4. **Write paths that break under concurrency or failure**: the bank return deletes and rewrites rows
   with no transaction, and proposal numbers collide.

## Recommendations

1. **Fix the security findings in the legacy system now** (S1 to S6). They are small, local changes and
   the exposure exists today, independent of any migration.
2. **Start the migration with billing** (*Cobrança*):
   - it carries the most divergent rules (R1 to R4), so it gains the most from a single implementation;
   - it touches money every day, so the parity tests pay for themselves;
   - it is contained: a handful of screens, one batch, one portal component.
3. **Decide the divergent rules with the business before migrating them**, with the numbers from the
   characterization tests: how many payments each choice changes and by how much.
4. **Keep the legacy system as the reference** until each slice is replaced: the new code is compared
   with the old code running, not with a document.

## Characterization

Characterization tests record what the legacy system does today, right or wrong, so that the new
system can be compared with it. For billing, the legacy code itself is executed as the oracle: the
VB.NET libraries and batch through a small .NET Framework 4.8 harness, the VB6 component through COM,
and the payment screen through a browser.

> In progress: the oracle harness and the characterization tests are being moved into this
> repository.
