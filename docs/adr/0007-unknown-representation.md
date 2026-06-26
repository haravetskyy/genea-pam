# ADR 0007 — Representing "Unknown": Explicit Member vs `null`

- **Status:** Accepted
- **Date:** 2026-06-26
- **Deciders:** Product owner
- **Related:** ADR 0003 (`LivingStatus` — explicit `Unknown` member), ADR 0004 (`GenderType` — `null` = unknown, no member), ADR 0006 (smart-enum implementation — *how* an enum is built; this ADR governs *whether* it carries an `Unknown` member). Hardening phase — codifies a modeling rule the system was already applying case-by-case.

## Context

For a closed enum (or any optional attribute), "the value is not known" can be modelled two ways:

- **`null`-as-unknown** — the column/field is nullable and `null` means "unknown / not recorded." The enum holds only positively-asserted members. (`GenderType`, ADR 0004.)
- **explicit `Unknown` member** — the type carries a named `Unknown` value; the field need not be nullable for the unknown case. (`LivingStatus`, ADR 0003.)

The codebase already contains **both**, each chosen for a stated reason — but the reasons were field-specific, never generalized. ADR 0004 argues `null`-as-unknown is "the honest shape" for gender; ADR 0003 argues an explicit `Unknown` is "the honest model" for living status. Without a written rule, the next enum is decided by vibes, and the dangerous failure mode is subtle: picking `null`-as-unknown for a field where **`null` already means something else**, silently conflating two distinct states. The live example is `DatePrecision` (ADR 0001): if `null` already means "there is no date to qualify," it cannot *also* mean "there is a date but its precision is unrecorded." One null slot, two unknowns.

This ADR records the decision rule so the choice is mechanical, not aesthetic.

## Decision

**Choose `null`-as-unknown only for a free-standing optional attribute whose `null` carries no other meaning. Otherwise use an explicit `Unknown` member.** Concretely, a closed enum gets an **explicit `Unknown` member** if *any* of the following hold; if none hold, use **`null`-as-unknown with no member**:

1. **There is a positively-asserted negative, or a real derived "unknown" state.** The domain lets a user assert a *fact* that is itself a kind of "no/none/unknown" — distinct from "nothing recorded yet" — or the value is *derived* and "unknown" is one of its genuine computed outcomes. Then three (or more) states exist and `null` can carry at most one. *Example:* `LivingStatus` — `Deceased` is the asserted negative of `Living`, and `Unknown` is a real Rule-X outcome (ADR 0003).

2. **`null` already carries a domain meaning for this field.** The field is nullable for a *different* reason — the attribute is not applicable, or it qualifies a parent field that can itself be absent — so the single `null` slot is already spoken for and cannot be overloaded to also mean "unknown." *Example:* `DatePrecision` — `null` means "no date to qualify"; "have a date, precision unrecorded" therefore needs its own representation.

3. **The field must be non-nullable** (schema or contract requires a value on every row). Then there is no `null` to use, and unknown must be a member.

Otherwise — a free-standing optional value, `null` meaning nothing else, no asserted negative (e.g. `GenderType`) — use **`null`-as-unknown, no `Unknown` member**. Adding one there reintroduces the null-vs-value double-state ADR 0003 and ADR 0004 reject (two ways to say "unknown": `null` *and* `Unknown`).

This is deliberately **per-field**, not a blanket "always include `Unknown`." *Rejected:* a blanket explicit-`Unknown`-everywhere rule — it forces a sentinel onto genuinely-optional fields (overturning ADR 0004's reasoned rejection of `Gender {…, Unknown}`), requires every such enum be made non-nullable to avoid the `null`-or-`Unknown` double-state, and makes `switch` exhaustiveness carry a meaningless arm. The per-field test costs three yes/no checks applied once when the enum is introduced; the blanket rule costs a sentinel on every row forever. *Also rejected:* a blanket "always `null`-as-unknown" — it silently breaks cases 1–3 (the `DatePrecision` conflation, the lost asserted-negative).

## Consequences

- **The choice is a checklist, run once per enum at introduction:** assert-negative-or-derived? → member. `null`-already-meaningful? → member. non-nullable? → member. Else → `null`-as-unknown. Record the answer in the enum's owning ADR/issue.
- **Existing decisions are consistent with this rule and unchanged:** `GenderType` (`null`-as-unknown — free-standing optional), `LivingStatus` (explicit `Unknown` — derived + asserted-negative). No code changes from this ADR.
- **`DatePrecision` (ADR 0001) is flagged by case 2.** When it is implemented (facts foundation, #90/#91), "have a date, precision unrecorded" must not be modelled as `null` precision (that collides with "no date"). Options at implementation time: a precision value that is only set when a date is set (precision lives with the date Fact, so `null` precision only ever co-occurs with `null` date and the conflation cannot arise), or an explicit `Unknown`/`Unspecified` precision member. The facts ADR/issue decides which; this ADR only flags that plain `null`-as-unknown is wrong here.
- **Interaction with ADR 0006:** orthogonal. ADR 0006 governs *how* a closed enum is built (smart enum: string + `CHECK` + `TryParse`, for request-bound enums); this ADR governs *whether* that enum includes an `Unknown` member and whether its column is nullable. An enum can be a smart enum with an `Unknown` member (`LivingStatus`) or a smart enum without one (`GenderType`).
- When a field that started as `null`-as-unknown later gains an asserted-negative (case 1 appears), migrating to an explicit member is a contract + possibly schema change — noted as the cost of the per-field rule, accepted because case-1 arrivals are rare and visible.
