# ADR 0003 — Living Status as Derived Three-State

- **Status:** Accepted
- **Date:** 2026-06-26
- **Deciders:** Product owner
- **Related:** requirements §5.3 (Living status, Confirmed deceased), §8.2 (node card), §9.2 (living-status search filter), §10 (privacy defaults differ for living persons); CONTEXT.md (Living status, Confirmed deceased — **updated by this ADR**); ADR 0001 (enum-as-string). Hardening phase — strengthening the Mini MVP (#58).

## Context

The graph endpoint computes living status as `IsLiving = p.DeathDate is null` (`GetTreeGraphEndpoint.cs:52`), surfaced as `GraphNode.IsLiving: bool`. This is the only living-status logic in the codebase, and it is wrong in two ways:

1. **A Person with no dates is asserted `IsLiving = true`** — reported as alive when the truth is unknown. A distant ancestor with no recorded death date shows as living.
2. **"Deceased, death date unknown" cannot be expressed.** There is no `Confirmed deceased` field on `Person` at all, despite CONTEXT.md and requirements §5.3 defining one. The system cannot represent "we know they died, we just don't know when."

CONTEXT.md previously defined Living status as a **two-state** property (Living *or* Deceased), which forces "no data" to collapse into "Living" — exactly the false-positive above. This ADR records the decision to make the property three-state and to fix the derivation, and updates CONTEXT.md accordingly.

## Decisions

1. **Living status is a three-state derived property: `Living` / `Deceased` / `Unknown`.** This intentionally **overrides CONTEXT.md's prior two-state definition** (CONTEXT.md is updated in the same change). Rationale: collapsing "nothing is known" into "Living" asserts a fact the data does not support; an explicit `Unknown` is the honest model and is what §10 privacy needs to avoid wrongly treating an unproven Person as living.

2. **Derivation is pure presence-of-data (Rule X) — no age heuristic.**
   - `Deceased` — `DeathDate` present **OR** `ConfirmedDeceased` set.
   - `Living` — **not** Deceased **AND** `BirthDate` present.
   - `Unknown` — **not** Deceased **AND** `BirthDate` absent.

   *Rejected:* an age-based presumption (e.g. born > 120 years ago ⇒ presumed Deceased, the MyHeritage/Ancestry "presumed deceased" behaviour). It is a new feature with a configurable threshold — scope creep for a hardening pass. Recorded as a possible deferred enhancement; the derivation seam stays open to it because the rule is a single pure function.

3. **Only `ConfirmedDeceased: bool` is stored; `LivingStatus` is never persisted.** It is computed by a single pure static function `LivingStatus.From(birthDate, deathDate, confirmedDeceased)` that every read path calls (graph, get-person, future search), killing the per-endpoint inlined `DeathDate is null` duplication. *Rejected:* (a) a persisted `LivingStatus` column — denormalized, drifts from its inputs, needs recompute on every date/flag write; (b) a `[NotMapped]` computed property on the `Person` entity — traps the logic on the entity and is not EF-translatable, so §9.2 search could not filter in SQL. The static function keeps Rule X expressible as an EF predicate (`death == null && !confirmed && birth != null`), leaving SQL-side filtering open.

4. **`LivingStatus` is an enum stored/serialized as string, used both in the domain and on the wire.** Consistent with ADR 0001's enum-as-string decision. Business logic (privacy §10, search §9.2) switches on the named cases. *Rejected:* `bool? IsDeceased` with `null` = Unknown — clever and compact, but the "null means Unknown" contract is a footgun every consumer must remember (truthiness checks silently conflate `null` and `false`, correct for the card but wrong for privacy). A self-documenting string enum is one representation end-to-end with no nullable-bool projection.

   > **Implementation:** built as a **smart enum per ADR 0006** (string-valued sealed class). Because `LivingStatus` is **derived** (`LivingStatus.From(...)`), never bound from untrusted request input, it needs only the sealed-class + string-serialization shape — **not** the `Parse`/`*Invalid`/`CHECK` machinery ADR 0006 specifies for request-bound enums. `Unknown` here is a real derived state (Rule X), not an absence sentinel, so it is a legitimate member.

5. **The graph DTO changes: `GraphNode.IsLiving: bool` → `GraphNode.Status: LivingStatus` (enum-string).** The node card (§8.2) collapses `Living` and `Unknown` (both render birth-year-only, no death year — "not visually distinguished"), but the DTO carries the full three-state value so the person drawer (§8.3) and privacy (§10) can distinguish Living from Unknown. *(Contract change to already-shipped code — to be applied in implementation, not in this documentation pass.)*

## Consequences

- `GetTreeGraphEndpoint.cs:52` (`IsLiving = DeathDate is null`) is replaced by `LivingStatus.From(...)`; the graph response field becomes `Status`.
- `Person` gains one column: `ConfirmedDeceased: bool` (default `false`).
- "Deceased, death date unknown" becomes representable via `ConfirmedDeceased`; a no-data Person resolves to `Unknown`, not falsely `Living`.
- **CONTEXT.md updated:** the Living status entry is rewritten from two-state to the three-state Rule X definition (done in this change).
- **Open contract follow-ups (not resolved here):**
  - **§9.2 search filter** is specified as binary "living / deceased". With three states it needs a third option or a documented mapping (does a "living" filter include `Unknown`?). Resolve when search is built; requirements §9.2 will need an edit.
  - The person-drawer and privacy DTOs do not exist yet; when built they consume `LivingStatus` directly (the reason the enum, not a card-only bool, is the domain type).
- This thread is independent of the handler/validation seam (ADR 0002) and the facts foundation (ADR 0001), though once facts land, `BirthDate`/`DeathDate` become Facts and `LivingStatus.From` reads the primary birth/death date Facts instead of flat columns — the function signature absorbs that change in one place.
