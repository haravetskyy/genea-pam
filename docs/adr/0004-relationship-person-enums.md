# ADR 0004 — Relationship & Person Enums

- **Status:** Accepted
- **Date:** 2026-06-26
- **Deciders:** Product owner
- **Related:** requirements §5.3 (Gender), §7.1 (Couple type), §7.2 (parentage type); CONTEXT.md (Couple, Filiation); ADR 0001 (enum-as-string storage convention), ADR 0002 (write/read seam). Hardening phase — strengthening the Mini MVP (#58).

## Context

Several model fields that should be closed contracts are stored as raw strings or are missing:

- **`Couple.Type`** is a required `string` hardcoded to `"Partner"` at every write (`CreateCoupleEndpoint`). `"Partner"` is not even one of the values §7.1 defines (`Married | Partners | Separated | Divorced | Other`). The field is required but the create request never accepts it — it cannot be set by a user.
- **`Person.Gender`** is a free `string?`. §5.3 defines a closed set.
- **`Filiation` parentage type does not exist as a field at all**, despite §7.2 stating "each child link has a parentage type" (`Biological | Adoptive | Step | Foster`).

ADR 0001 already established the storage convention for enums (C# enum, `HasConversion<string>()`, plus a `CHECK (col IN (...))` on closed-enum columns). This ADR applies that convention to these three fields and records the value-set and default decisions specific to each. It does **not** touch the structural questions of Couple/Filiation (nullable second person, single-parent support, person-deletion cleanup, slot normalization) — those are explicitly shelved for a separate relationships-structure pass.

## Decisions

1. **Storage convention is inherited from ADR 0001, not re-decided.** All three enums are C# enums persisted via `HasConversion<string>()`, each closed-enum column additionally guarded by a `CHECK (col IN (...))` constraint. Strings are readable/GEDCOM-friendly/reorder-safe; the CHECK recovers database-level rejection of bad values.

2. **`Couple.Type` becomes `CoupleType { Married, Partners, Separated, Divorced, Other }` — non-nullable, default `Partners`, settable at create.** The `CreateCouple` request gains an optional `Type`; when omitted it defaults to `Partners` (the neutral, least-committal member — it asserts neither marriage nor divorce/separation, matching the intent of the old `"Partner"` string). *Rejected:* keeping it server-hardcoded — that would convert the field to an enum nobody can set, the same "structurally present but unusable" smell as the empty `PersonFact` stub rejected in ADR 0001. §7.1 frames relationship type as a user choice, so the create path accepts it.

3. **`Person.Gender` becomes `GenderType { Male, Female, Other }` — nullable, where `null` means unknown/not-recorded.** The enum holds only positively-asserted values; "unknown" is the absence of a value (`null`), not a sentinel member. *Rejected:* (a) a non-nullable enum with an `Unknown` member defaulting to `Unknown` — forces an asserted value onto every row when gender is genuinely optional; (b) a nullable enum that *also* has an `Unknown` member — reintroduces the null-vs-value double-state that ADR 0003 eliminated for living status. Unlike living status (always *some* derived state), gender is legitimately optional, so modelling absence as `null` is the honest shape.

   **Deviation from §5.3:** the requirement literally lists `Male, Female, Other, Unknown` as four values; this ADR maps `Unknown → null` rather than an enum member. The UI may still present an "Unknown"/"Prefer not to say" option, which simply means "leave unset". Recorded here as an intentional deviation, analogous to ADR 0003's three-state override of CONTEXT.md.

4. **`Filiation` gains `ParentageType { Biological, Adoptive, Step, Foster }` — non-nullable, default `Biological`, settable at create.** §7.2 states every child link has a parentage type, so the field is required; there is no meaningful "unknown parentage" state (linking a child to parents asserts *some* relationship, and biological is the default assumption). *Rejected:* (a) nullable parentage — §7.2 says every link has a type; (b) non-settable/hardcoded — leaves an unsettable enum (same half-fix rejected for `Couple.Type`).

   > **Superseded shape (see ADR 0005):** this decision originally placed `ParentageType` on a child→couple Filiation. ADR 0005 re-architects the Filiation into a child→**parent** edge (per-parent parentage). The `ParentageType` *enum* (values, non-nullable, default `Biological`, settable at create) is unchanged and still recorded here, but it now lands on the per-parent Filiation, and its delivery moved out of the enums PRD (#100) into the re-architecture PRD (#101), where the Filiation it sits on is reshaped.

## Consequences

- Three migrations/columns: `couples.type` converted to the `CoupleType` enum string + CHECK; `persons.gender` converted to the `GenderType` enum string + CHECK (nullable); `filiations.parentage_type` added as `ParentageType` enum string + CHECK, non-null default `Biological`.
- `CreateCouple` and `AddFiliation` request contracts each gain one optional enum field. `Person` create/update already carry `Gender`; the type changes from `string?` to `GenderType?`.
- **`Couple.Type` is settable at create but not editable yet** — there is no `UpdateCouple` endpoint today. Editing a couple's type (e.g. Partners → Married, or → Divorced) is deferred until an update endpoint exists. Noted, not built here.
- **Gender `Unknown → null` deviation from §5.3** is intentional and recorded above; any §9.2 gender search treats "unknown" as `gender IS NULL`.
- The Couple/Filiation **structure** is unchanged by this ADR: `PersonBId` stays required (no single-parent support yet), person/child FKs stay `Restrict` (the §5.5 delete-cleanup contradiction is **not** resolved here), and no slot-normalization CHECK is added. These remain open in a future relationships-structure thread.
- **Sequencing:** these enum changes touch the same Couple/Person/Filiation write paths as the ADR 0002 seam conversions (#97 Trees, #98 Persons, #99 Couples). They are cleanest layered *on top of* those conversions (validators enforce enum membership at the edge, commands carry the typed enum) rather than racing them. Recommended order: ADR 0002 seam for an area → then its enum change. When facts land (ADR 0001), Couple start/end dates and similar move to Facts, but the relationship-*type* enums here are unaffected.
