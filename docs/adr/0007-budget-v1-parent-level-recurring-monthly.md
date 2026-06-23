---
status: accepted
---

# Budget v1 is a parent-level recurring monthly limit; per-month history deferred

The chart suite is retrospective by design (ADR-driven), with one deliberate
forward-looking investment: a per-category **Budget** that unlocks a
budget-vs-actual chart and a pace overlay. No budgeting primitive exists today
("budget" appears only in doc comments and AI prompt text). We decided the
smallest Budget shape that delivers the chart without painting us into a corner.

We chose: **`Budget(Category, Amount)` — an opt-in, recurring monthly limit on a
top-level (parent) category**, with exactly one row per category. Per-month budget
history and leaf-level budgets are explicitly deferred but kept as a clean future
extension.

## Considered options

- **Attach to parent vs leaf categories.** Chose **parent**. Budgets compared at
  the parent-rollup level match `ReportingRow.EffectiveCategory`, so "actual" is
  computed by the same grouping as every other chart — no separate aggregation
  path, fewer rows to maintain. Leaf-level budgets ("Dining out: $200") are more
  precise but require their own rollup and more manual upkeep; deferred.

- **Single recurring amount vs per-month overrides.** Chose **single recurring
  amount**. One `Amount` interpreted as the same limit each month is the minimum
  that drives the chart. Per-month overrides (a time series of plans) add real
  complexity for a feature whose value is "am I within budget *this* month".

- **Build the per-month-history model now (rejected for v1, designed-for).** The
  user asked to keep per-month history (Y) possible. Rather than build it, v1 is
  defined as the special case of the future model: the recurring row is "the budget
  effective until changed". Y is introduced later via effective-dated rows
  (`EffectiveFrom`) or an override table, resolving "budget for month M" as the
  latest row with `EffectiveFrom <= M`. v1 adds no column that blocks this, so the
  later change is additive.

## Consequences

- A `Budget` entity + `DbSet<Budget>` is added (no EF migration files exist today;
  schema derives from `DataContext`). One row per parent category, opt-in.
- Budget-vs-actual and the cumulative-spending pace overlay consume the existing
  `ReportingRow` projection filtered to the month; no new aggregation rule.
- Per-month history (Y) and leaf-level budgets are separate future candidates that
  **supersede** this ADR rather than reverse it. v1 must not bake in assumptions
  (e.g. a hard 1:1 Category↔Budget uniqueness enforced in ways that block
  effective-dated rows) that would make Y a breaking change.
- Categories without a budget are absent from budget views — there is no implicit
  zero or global cap in v1.
