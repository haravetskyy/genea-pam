# Genea-PAM — Project Context

A family tree platform targeting semi-serious family historians. Users build and share genealogical trees composed of Persons and their relationships.

## Language

**User**:
A registered account on the platform — has credentials, a display name, and account settings.
_Avoid_: Account, member, participant

**Person**:
A genealogical individual who exists inside a family tree — has facts, relationships, and a profile within that tree. May or may not correspond to a real User on the platform.
_Avoid_: Individual, member, record

**Living status**:
A derived two-state property of a Person: Living (no death date Fact and Confirmed deceased not set) or Deceased (has a death date Fact, or Confirmed deceased is set). Overridable via the Confirmed deceased flag.
_Avoid_: Status, alive status, death status

**Timeline event**:
A typed life event on a Person's timeline, stored as a Fact record. Either auto-derived from existing Facts (birth, marriage, death) or manually added (military service, immigration, education, etc.). Has an optional date, place, description, and Citation.
_Avoid_: Event, life event, timeline entry

**Duplicate candidate**:
A Person surfaced by duplicate detection as a potential match for a Person being added or edited — same Tree, similar name, approximate birth year. Requires explicit User resolution; never auto-merged.
_Avoid_: Duplicate, match, potential duplicate

**Root person**:
The Person designated by the Tree owner as the default center of the graph on first load. Stored on the Tree record. Defaults to the Tree creator if not explicitly set.
_Avoid_: Focus person, home person, default person, anchor person

**Active person**:
The Person currently centered in the graph viewport. Ephemeral frontend state — not persisted. Changes when the user clicks a mini-card or navigates via a shareable URL.
_Avoid_: Selected person, current person, centered person

**Mourning ribbon**:
A black overlay applied to a deceased Person's profile photo. Has a tree-level default (on/off for all deceased Persons) and a per-Person override. Never available for Living Persons.
_Avoid_: Death overlay, ribbon, mourning overlay

**Naming convention**:
A cultural rule for the display order of name components, associated with one or more nationalities. Values: `Western` (given name first) and `Eastern` (surname first). Used as an input in name order resolution.
_Avoid_: Name convention, name culture, locale

**Name order**:
The resolved display order of a Person's name components: `GivenFirst` or `SurnameFirst`. Resolved via a four-level priority chain: per-person override → naming convention derived from nationality → tree default → user locale.
_Avoid_: Name display order, name format

**Display name**:
The User-chosen handle shown on the platform (nickname). Separate from login email and from any Person record.
_Avoid_: Username, nickname, handle

**Full name**:
The composite name of a Person: first name, last name, patronymic, matronymic. Displayed according to the name order resolution rules. Distinct from alternate names (maiden name, alternative spelling, nickname).
_Avoid_: Name, person name

**Date precision**:
An enum on a date Fact indicating how precisely the date is known: `FullDate`, `MonthYear`, `YearOnly`, or `Approximate`.
_Avoid_: Date accuracy, date granularity, precision level

**Confirmed deceased**:
A boolean flag on a Person explicitly set by the User to indicate the Person is deceased when no death date Fact exists.
_Avoid_: Presumed deceased, death confirmed, deceased override

**Citation**:
A record attached to a Fact that documents its source (source name, URL, date accessed, page/reference, notes). A Fact can have multiple Citations.
_Avoid_: Source citation, source, reference

**Cross-tree link**:
A proposed and accepted assertion that a Person in one Tree is the same individual as a Person in another Tree. The two Trees remain fully independent; the link is navigable only. Requires acceptance by the other Tree's owner.
_Avoid_: Soft link, connection, match

**Merge**:
An irreversible operation that imports Persons and Relationships from a source Tree into a target Tree. Distinct from a Cross-tree link — a Merge physically consolidates data; a Cross-tree link does not.
_Avoid_: Hard merge, hard link, import

**Membership**:
A record associating a User with a Tree, carrying a role (Owner, Editor, Viewer). Every User with any access to a Tree has exactly one Membership. The Owner is a Membership with role Owner.
_Avoid_: Access grant, collaboration, role (when referring to this construct)

**Filiation**:
The record linking a Person (as child) to a Couple, carrying a parentage type (Biological, Adoptive, Step, Foster). A Person can have multiple Filiations with different parentage types.
_Avoid_: Child link, parentage, child record

**Couple**:
A pairing of two Persons (or one, for single-parent relationships) with a type (Married, Partners, Separated, Divorced, Other), optional start and end dates, and zero or more Filiations. The structural unit through which parent–child relationships are expressed.
_Avoid_: Union, partnership, relationship (when referring specifically to this construct)

**Tree**:
The top-level container a User creates, names, and owns. Contains Persons, their Facts, their Couples, and their Filiations. Has a visibility setting (public or private) and a designated owner.
_Avoid_: Family tree, genealogy, archive

**Fact**:
A typed, sourced datum attached to a Person (e.g. birth date, birth place, death date). Stored as a discrete record, not a flat field.
_Avoid_: Field, attribute, property

**Primary fact**:
The fact record designated by the User as authoritative when multiple facts of the same type exist for a Person.
_Avoid_: Main fact, default fact

**Alternate fact**:
A non-primary fact record of the same type on the same Person — typically from a conflicting source.
_Avoid_: Secondary fact, duplicate fact, candidate fact

## Git Flow

| Branch | Purpose |
|---|---|
| `feature/I{n}/{slug}` | Implementation of issue #n |
| `fix/I{n}/{slug}` | Bug fix tied to issue #n |


- Branch off `develop` for features and fixes
- `{n}` is the GitHub issue number (e.g. `I2`)
- `{slug}` is a short, lowercase, hyphenated description of the issue (e.g. `monorepo-and-docker-compose`)
- PRD issues (parent issues that group sub-issues) do not get branches
