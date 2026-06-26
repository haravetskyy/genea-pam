# ADR 0001 — Facts & Citations Foundation

- **Status:** Accepted
- **Date:** 2026-06-26
- **Deciders:** Product owner
- **Related:** PRD #89; slices #90, #91, #92, #93, #94; requirements §5.1, §6, §7.1, §8.4; CONTEXT.md (Fact, Primary fact, Alternate fact, Citation, Date precision)

## Context

The Mini MVP (#58) shipped a thin CRUD shell: `Person` carries flat `BirthDate`/`DeathDate`/`*DatePrecision` (string) columns, `PersonFact` is a dead stub (`Id` + `TreeId` only), and citations were cut entirely. The domain language (CONTEXT.md, requirements §5.1) describes a fact-based model — discrete typed, sourced fact records with primary/alternate semantics — that does not exist in code.

This ADR records the structural foundation for facts and citations. This is a **foundation pass that also wires citations end-to-end (backend only)** — facts (Person birth/death, Couple start/end), the reusable `Source`, the `Citation` join, and primary/alternate semantics all get real write paths. Citations are pulled in now because their design is settled (requirements §6) and a fact you cannot source is barely more than the flat column it replaces. The goal is still bounded: it front-loads the schema decisions that are expensive to reverse once the `facts` table is populated (polymorphic owner, Citation → Source foreign key), and it deliberately leaves out everything not needed to make the fact model honest — **place-fact endpoints, attribute facts (occupation/nationality/religion), the timeline, manual timeline events, the frontend source-reuse typeahead, and GEDCOM export are all out of scope**. The `FactType` enum holds those values, but no endpoint in this pass writes them.

We surveyed how established systems model this:

- **GEDCOM 7** — splits time-framed events (`BIRT`, `DEAT`, `MARR`) from attributes (`OCCU`, `RELI`); citations (`SOUR`) are pointers to reusable `SOURCE_RECORD`s.
- **Gramps** — `Citation` is a join object connecting a reusable `Source` to any record; hard distinction between Events (dated) and Attributes (stable, singular). Attributes are rarely independently cited — the citable unit is the Event.
- **GEDCOM-X** — the persona/conclusion *evidence model* is the genealogically "correct" ceiling, but a different data model entirely; deliberately out of scope.
- **MyHeritage / Ancestry** — pragmatic "preferred/alternate" facts: keep conflicting values, mark one primary. This is the middle ground requirements §5.1 adopts.

## Decisions

1. **One real `Fact` table; the `PersonFact` stub is deleted.** Fact value storage = typed nullable columns in a single table with a **polymorphic owner** (`owner_person_id?` *or* `owner_couple_id?`), so a Fact can belong to a Person or a Couple (§7.1 couple dates are facts).

2. **Source and Citation are split; Source is reusable.** A `Source` (Tree-owned: name, url, repository) is cited via a `Citation` join (`fact_id`, `source_id`, page, date accessed, notes) — "cite once, reference many". *Rejected:* inlining source fields into each Citation (the MyHeritage-lite shape, requirements §6 literal) — it forces a later migration on a populated table plus a de-duplication heuristic, and requirements §6/§11 and post-MVP record-matching/media all presuppose a reusable Source. A source-reuse typeahead is part of the foundation conceptually (the split is pointless without reuse), though the **frontend** typeahead is deferred to the FE phase.

3. **Event vs Attribute is one physical table, distinction enforced in the domain layer.** Events (birth/death/marriage — dated, place, timeline-feeding) and Attributes (gender/nationality/religion/occupation — stable, `text_value`) share the `facts` table, distinguished by `FactType`. A `FactKind(FactType) -> Event | Attribute` rule plus validators (e.g. reject a date on an attribute fact) live in the handler/domain layer. *Rejected:* (a) attributes as flat uncitable columns — violates §6's per-field "Add source"; (b) two physical tables (Fact + Attribute) — would make Citation polymorphic over Fact|Attribute (four owner foreign keys across the graph, two cascade paths, union queries). Citation therefore has a single owner: `fact_id`.

4. **Fact owner integrity is a database CHECK, not EF Single Table Inheritance.** `CHECK ((owner_person_id IS NULL) <> (owner_couple_id IS NULL))` guarantees exactly one owner at the database, for every write path. Both owner foreign keys are `OnDelete.Cascade` (§5.5). One plain `Fact` C# class. *Rejected:* STI (new EF pattern, makes the hot "all facts for a person" query an `.OfType<>()`, and doesn't protect raw SQL anyway); app-layer-only validation (the invariant rots under any bypassing write path). This matches the repo's existing DB-level integrity instinct (`CoupleConfiguration` uses Restrict/Cascade foreign keys).

5. **Source deletion is Restrict-while-cited; Tree deletion cascades.** `Citation.source_id` foreign key is `OnDelete.Restrict` — deleting a Source that still has Citations returns 409; the user detaches it first. Provenance is never silently destroyed. Deleting the whole Tree cascades Sources and Citations (§4.3). `Source.tree_id` is `OnDelete.Cascade`.

6. **Enums are stored as strings, with CHECK constraints on closed-enum columns.** Persisted as a readable string (GEDCOM/debug-friendly, reorder-safe). Closed enums (`DatePrecision`, `Gender`, `Couple.Type`, `Filiation` parentage) additionally get a `CHECK (col IN (...))` to recover database-level rejection of bad values. *Rejected:* int storage (opaque rows; reordering members corrupts data); native Postgres enum types (`ALTER TYPE`-per-value friction is disqualifying for a growing `FactType`, plus Npgsql wiring). `FactType` is a **closed enum with an `Other` member + nullable `custom_label`** (required iff `Other`), keeping known types type-safe and queryable while supporting §8.4's free-text "Other" timeline events.

   > **Implementation:** the C# type backing each closed enum is a **smart enum per ADR 0006** (a string-valued `sealed` class with `TryParse`), not a plain `int`-backed `enum` — this realizes the "stored as string + CHECK" contract above while giving request-bound enums RFC 9457 422 on unknown input. See ADR 0006 for the full convention.

## Consequences

- The schema can hold conflicting/primary facts, place facts (text + geocoordinates), couple-date facts, citations, and typed timeline events. *Written* in this pass: Person birth/death facts, Couple start/end facts, Sources, Citations (create-or-reuse + delete), and primary designation. *Not written:* place facts, attribute facts, and manual timeline events — those become cheap additions, not migrations. The frontend source-reuse typeahead is backed by the Source search endpoint but the UI itself is deferred to the FE phase.
- The two CHECK constraints (exactly-one-owner; closed-enum membership) carry the integrity that a polymorphic single table would otherwise leak.
- Frontend source-reuse typeahead, relationships-API scalability, living-status derivation, the handler-layer extraction, and the validation layer are **separate hardening threads**, not part of this ADR.
- GEDCOM export (§11) and post-MVP record matching map cleanly: `Source` → `SOUR` record, `Citation` → `SOUR` pointer.
