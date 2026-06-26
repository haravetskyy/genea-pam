# ADR 0005 — Parent–Child Re-architecture (Filiation as Child→Parent Edge)

- **Status:** Accepted
- **Date:** 2026-06-26
- **Deciders:** Product owner
- **Related:** requirements §5.5 (person deletion), §7.1 (relationship types, single parent), §7.2 (parentage type, child in multiple relationships, sibling derivation), §8.3 (drawer relationships), §1.4 (account deletion); CONTEXT.md (Couple, Filiation — **rewritten by this ADR**); ADR 0001 (couple-date Facts, enum-as-string, DB-CHECK integrity), ADR 0004 (ParentageType enum). Hardening phase, but this thread is a deliberate **re-architecture** of the parent–child core, larger than a hardening tweak.

## Context

The Mini MVP modelled parenthood through the Couple: `Couple(PersonA, PersonB)` with both persons required, and `Filiation(child, couple)` linking a child to the *pairing*. This contradicts the requirements in several ways:

- §7.1 requires single-parent relationships — impossible with a required second person.
- §7.2 requires per-parent parentage: a child can be biological to one parent and step/adoptive to the other in the same household. A single parentage type on the child→couple link cannot express this asymmetry.
- §5.5 requires deleting a person to remove their relationship participation, but the person FKs are `Restrict`, which *blocks* deletion — a direct contradiction.

An earlier exploration tried to patch the couple-container model (nullable second person, a slot-A normalization CHECK, "promote B into A" on delete, handler-orchestrated cleanup). That machinery existed only to keep parenthood flowing through the couple's two person slots. This ADR removes the root cause instead: it decouples parenthood from the Couple entirely.

We surveyed prior art: **GEDCOM** and **Gramps** both model the parent–child link per individual parent (GEDCOM's `PEDI` pedigree sits on each child→parent link, not on the family), and treat the family/union as a separate grouping. This ADR adopts that normalized shape.

## Decisions

1. **The Filiation is an independent child→parent edge, not a child→couple link.** `Filiation(childPersonId, parentPersonId, ParentageType)`. A two-parent child has two Filiations; a single-parent child has one. Parentage is **per-parent**, so asymmetric parentage (Biological to one parent, Step to the other) is expressible (§7.2). The `ParentageType` enum is unchanged from ADR 0004 (`Biological | Adoptive | Step | Foster`, default `Biological`).

2. **The Couple is a union record only, decoupled from parenthood.** A Couple is a marriage/partnership between two Persons carrying the relationship type (ADR 0004 `CoupleType`) and optional start/end date Facts (ADR 0001). It contains no children and does not express parent–child relationships. *This supersedes* the prior model and the earlier provisional decisions (couple-as-sole-parent-container, nullable `PersonBId`, slot-A normalization CHECK, promote-B-into-A) — all dissolved.

3. **Co-parenthood is derived, not stored.** A Filiation carries no Couple reference. That two parents form a unit is derived when needed: two parents who share a child and are also in a Couple were partners. *Rejected:* an optional `coupleId` on the Filiation — it re-introduces the couple link onto the edge this ADR deliberately decoupled.

4. **Siblings derive from shared parent, not shared Couple.** Two Persons who share a parent (a Filiation to the same parent Person) are siblings; half-siblings (one shared parent) surface correctly. This replaces the prior "children of the same Couple" rule.

5. **Couple person slots are interchangeable.** PersonA and PersonB carry no role, gender, or primacy meaning. (Mother/father is a display concern derivable from each Person's Gender; it is not a slot property.)

6. **Filiation integrity (DB-level, per the ADR 0001 instinct):**
   - `CHECK (child_person_id <> parent_person_id)` — no self-parenthood.
   - `UNIQUE (child_person_id, parent_person_id)` — a parent appears once per child. This forecloses modelling the same person as both Biological *and* Adoptive parent of one child via two rows; that case is rare and can be revisited by relaxing the constraint later.
   - The Filiation gains its **own `TreeId`** FK (it no longer inherits tree scope through a Couple). A validator asserts child and parent are both in that Tree.

7. **Person deletion (§5.5) is a hard delete with cleanup intent, resolved entirely by cascading FKs.** Deleting a Person physically removes the row. Every relationship FK is `OnDelete.Cascade`: `Filiation.childPersonId`, `Filiation.parentPersonId`, `Couple.PersonAId`, `Couple.PersonBId`, and every `TreeId`. Therefore `DeletePerson` needs **no relationship-cleanup handler code** — the §5.5-vs-`Restrict` contradiction dissolves into plain cascades. Deleting a partner deletes the Couple union record (and its start/end date Facts). *Rejected:* (a) `Restrict` (the current state, blocks §5.5); (b) `SetNull` on couple slots to preserve one-person "ghost" unions, and (c) conditional keep-if-other-partner-exists — both re-introduce slot nullability and a cleanup handler, the machinery model Y removes, for a history-preservation benefit that belongs to a different intent.
   - **§1.4 account deletion is unchanged** and separate: it cascades via tree-deletion per the existing requirement ("trees not transferred are deleted along with the account").

## Consequences

- **CONTEXT.md rewritten:** the *Filiation* entry now defines a child→parent edge with per-parent parentage and derived co-parenthood; the *Couple* entry now defines a union record decoupled from parenthood with interchangeable slots. (Done in this change.)
- **Schema:** `filiations` changes from `(child, couple)` to `(child_person_id, parent_person_id, parentage_type, tree_id)` with the self-parent CHECK, the `(child, parent)` UNIQUE, and all-cascade FKs. `couples` keeps its two person slots (both still required — single-parent is no longer a couple concern) with cascade person FKs. This is a **breaking migration** of the filiations table.
- **Graph DTO change:** parent–child edges are now `parent→child`, not `couple→child`. The graph's `GraphEdge` filiation shape (currently `CoupleId` + `ChildId`) becomes `parentPersonId` + `childPersonId`. Couple edges still render as person↔person union edges. *(Contract change to shipped code — applied in implementation, not in this documentation pass.)*
- **Sibling derivation** (when built for the drawer §8.3) queries shared-parent, not shared-couple.
- **The deletion model is settled for the MVP as hard-delete/cleanup.** Soft-delete / anonymize (erasure that preserves tree coherence — relevant to §10 privacy and any GDPR-style requirement) is explicitly deferred to its own future thread; it is the right tool for "preserve marriage/parent history through deletion," which this ADR intentionally does not do.
- **Single parent** is now trivial: one Filiation row, no nullable slots, no normalization CHECK, no promotion logic.
- **Sequencing:** this re-architecture touches the Couples/Filiations write paths and the graph read path; it layers on the ADR 0002 seam (commands/handlers, static queries, inline validation) and is consistent with the ADR 0004 enums (CoupleType on the union, ParentageType on the per-parent Filiation). Couple start/end dates remain ADR 0001 Facts owned by the Couple.
