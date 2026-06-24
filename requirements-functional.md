# Genea-PAM — Functional Requirements

## Product Vision

A modern family tree platform targeting semi-serious family historians. The north star is "a better MyHeritage" — fresher, less bloated, with an intuitive UI where every operation is discoverable without external help.

---

## MVP

### 1. Account Management

#### 1.1 Registration

- Users can register with an email address and password
- Users can register and log in via Google OAuth
- OAuth users do not set a password; identity is tied to the provider token

#### 1.2 Password Reset

- A registered user can reset their password via a time-limited link sent to their registered email address

#### 1.3 Transactional Emails

The system sends the following automated emails:
- Registration confirmation
- Password reset link
- Account deletion confirmation

#### 1.4 Account Deletion

- A user can permanently delete their account after explicit confirmation
- Before deletion is finalized, the user is offered the option to transfer ownership of their trees to another registered user
- Trees owned by the user that are not transferred are deleted along with the account
- Trees the user participates in as a collaborator (but does not own) are not affected

### 2. User Profile

- A user can set and update a display name (nickname)
- A user can upload a profile avatar image
- A user can provide a contact email separate from their login email; this field is opt-in, hidden by default, and toggleable at any time
- A user can set their preferred UI language in account settings

### 3. Localization

- The UI is available in English, Ukrainian, and Polish at launch
- A language switcher is available in the header and accessible to guests without an account
- Date display follows the user's locale preference

### 4. Family Tree Management

#### 4.1 Creating a Tree

A user can create a new family tree. The creation form has the following fields:
- Tree name (required)
- Description (optional)
- Icon / cover image (optional, see section 5.6 for supported file types)
- "I am not a member of this tree" checkbox (unchecked by default)

A newly created tree is private by default.

#### 4.2 Editing a Tree

The tree owner can:
- Edit the tree name, description, and icon at any time
- Set tree visibility to public or private:
  - **Public:** the tree is viewable by anyone, including guests without an account
  - **Private:** the tree is invisible to all users unless explicitly shared with them
- Set the default visualization mode for the tree
- Set the default color theme for the tree

Any logged-in user can override the tree's default visualization mode and color theme for themselves; the preference is saved to their account.

A guest can override visualization mode and color theme locally in the browser for the duration of the session.

#### 4.3 Deleting a Tree

The tree owner can permanently delete a tree after explicit confirmation. Deletion removes all persons, relationships, facts, source citations, and media associated with the tree. This operation is irreversible.

#### 4.4 Empty State and Onboarding

- On first tree creation, the user is offered a guided "Add yourself" onboarding flow with a simplified person form (name, birth year, gender)
- If the user skips onboarding, the empty graph canvas displays a large centered "+" button captioned "Add first tree member"
- A lightweight dismissible tooltip overlay is shown on first tree creation, explaining the "+" handle interaction on person nodes

### 5. Person Management

#### 5.1 Fact-Based Data Model

Person data that represents a fact (birth date, birth place, death date, death place, marriage date, etc.) is stored as a discrete typed fact record rather than a flat field on the person record. Each fact record stores: type, value, date precision level, approximate flag, and geocoordinates (for place facts).

A person can have multiple fact records of the same type (e.g. two conflicting birth dates from different sources). The user designates one as primary.

#### 5.2 Adding a Person

A person can be added to a tree via two entry points:
- **Graph-first (primary):** clicking a "+" handle on an existing person node in the graph, pre-wired with relationship context
- **Form-first (secondary):** a standalone "Add person" action for adding a person with no immediate relationship context

The person form uses progressive disclosure: required fields are prominent at the top, optional fields are available on scroll without wizard steps.

#### 5.3 Person Fields

**Primary name** (always visible in the form, not behind disclosure):
- Given name — optional; if the person has only one name (mononym), leave this blank and set the single name as the surname
- Surname — optional; for the Iberian dual-surname system, this field holds the paternal surname (primer apellido)
- Maternal surname — optional; used for the Iberian system (segundo apellido, derived from the mother's paternal surname) and the Portuguese/Brazilian reversed order; hidden by default, revealed when the person's nationality resolves to a dual-surname convention or when the user explicitly adds it
- Patronymic — optional; entered manually, derived from a parent record with explicit user confirmation, or left empty
- Matronymic — optional; same rules as patronymic
- Mononym — boolean flag; when set, the UI treats the surname as the sole name and suppresses the "given name" field entirely; the "first name required" validation is lifted

**Alternate names** — a list of additional names, each with a type:
- Maiden name
- Alternative spelling
- Nickname
- Romanization — a Latin-script rendering of a name stored in another script (e.g. Pinyin for Chinese, Hepburn for Japanese)
- Religious name — a name taken at baptism, confirmation, ordination, or equivalent rite
- Clan or tribal name — a clan, sept, or tribal affiliation used as a name component (e.g. Navajo clan names)

**Identity fields** (visible in the form by default):
- Gender — Male, Female, Other, Unknown
- Nationality — optional free text with ISO country autocomplete; supports historical values (e.g. "Austro-Hungarian", "Ruthenian")
- Religion — optional free text with common religion autocomplete; sensitive field, hidden by default in privacy settings
- Occupation — optional free text

**Fact fields** (stored as discrete fact records):
- Birth date — with precision: full date / month + year / year only / approximate year
- Birth place — free text with optional geocoordinate pin
- Death date — with precision: full date / month + year / year only / approximate year
- Death place — free text with optional geocoordinate pin

**Derived fields:**
- Living status — derived automatically from the presence of a death date; the user can override this manually when the death date is unknown but the person is confirmed deceased

**Other fields:**
- Biography — free text, visible to all users with access to the tree
- Notes — free text, private to the author; never visible to any other user, including the tree owner

**"This is me" checkbox:**
- Appears in the add-person form for an owner or contributor until they have a linked person record in the tree
- If the tree was created with "I am not a member of this tree" checked, this checkbox never appears for the creator in that tree

#### 5.4 Editing a Person

The owner can edit any field of an existing person record at any time.

#### 5.5 Deleting a Person

The owner can delete a person after explicit confirmation. Deletion removes the person record, all their fact records, their media, and their participation in any relationships.

#### 5.6 Profile Photo

The owner can upload and set one profile photo per person. The profile photo is displayed in graph node cards and the person drawer.

Supported formats: JPEG, PNG, WebP.

#### 5.7 Mourning Ribbon

The owner can add a black mourning ribbon overlay to a deceased person's profile photo. This option is available only for persons with confirmed deceased status; it is never available for living persons.

The mourning ribbon can be:
- Toggled globally for the whole tree (applies to all deceased persons by default)
- Overridden per person (on or off regardless of the global setting)

#### 5.8 Duplicate Detection

When adding or editing a person, the system checks for existing persons in the same tree with a similar name and approximate birth year. If a potential duplicate is found, a non-blocking warning is shown:

> "A person named [X] born around [Y] already exists in this tree. Is this the same person?"

The user can dismiss the warning and proceed, or navigate to the existing person. No automatic merging occurs — all resolution requires explicit user action.

#### 5.9 Name Order

The display order of name components (given name first vs. surname first) is resolved in the following priority, from highest to lowest:

1. Per-person override — explicitly set on the person record
2. Derived from the person's nationality — if a known convention exists for that nationality (see table below)
3. Tree-level default — set by the owner for the whole tree
4. User's locale — the browser or account locale as a final fallback

**Known naming conventions by nationality (MVP list):**

| Convention | Nationalities | Display format |
|---|---|---|
| Western (given first) | Default for all nationalities not listed below | Given + Surname |
| Eastern (surname first) | Chinese, Japanese, Korean, Vietnamese, Hungarian | Surname + Given |
| Iberian dual-surname | Spanish, most Latin American | Given + Paternal surname + Maternal surname |
| Portuguese/Brazilian dual-surname | Portuguese, Brazilian | Given + Maternal surname + Paternal surname |
| Slavic three-part | Ukrainian, Belarusian, Bulgarian, Serbian | Given + Patronymic + Surname |
| Icelandic patronymic | Icelandic | Given + Patronymic (no inherited family surname) |
| Mononym | Javanese and others flagged with the mononym field | Surname only |

The nationality field drives convention lookup; historical nationality values (e.g. "Austro-Hungarian") map to the modern convention of the primary successor state unless overridden at the per-person level.

#### 5.10 Patronymic and Matronymic Derivation

When a parent link exists on the person record, the system may suggest the parent's given name as a derivation hint for the patronymic or matronymic. Derivation is always opt-in — the user explicitly confirms or dismisses the suggestion.

The system does not append any suffix automatically. The user enters the full patronymic or matronymic themselves (e.g. "Іванович", "Іванівна"). The suggestion only pre-fills the parent's given name as a starting point.

Known conventions by nationality (MVP list):

| Nationality | Derivation hint provided |
|---|---|
| Ukrainian, Belarusian | Father's given name (for patronymic); mother's given name (for matronymic) |
| Bulgarian, Serbian | Father's given name (for patronymic) |
| Icelandic | Father's given name (for patronymic); mother's given name (for matronymic) |
| Arabic | Father's given name (for patronymic) |

If the person's nationality does not match a known convention, no hint is offered; the user can still enter a value manually.

### 6. Source Citations

Each fact record can have one or more source citations attached to it. A source citation has the following fields:
- Source name — free text, required
- URL — optional
- Date accessed — optional
- Page / reference — optional
- Notes — optional

An "Add source" affordance is exposed next to each fact field in the person form.

Full structured citations (repository type, archive, document type, confidence level) are post-MVP.

### 7. Relationships

#### 7.1 Creating a Relationship

The owner can create a relationship between two persons in the same tree. Supported relationship types: Married, Partners, Separated, Divorced, Other.

Single-parent relationships (with no second person) are supported.

A relationship can have an optional start date and end date, each with a precision level and approximate flag (stored as fact records).

#### 7.2 Adding Children

The owner can add a child to a relationship. Each child link has a parentage type: Biological, Adoptive, Step, or Foster.

A child can belong to multiple relationships with different parentage types (e.g. biological parents and adoptive parents).

Siblings are derived automatically as persons who share a relationship — no separate sibling record is stored.

#### 7.3 Removing and Deleting Relationships

- The owner can remove a child from a relationship without deleting the person record
- The owner can delete a relationship after explicit confirmation; this does not delete the persons involved

### 8. Tree Visualization

#### 8.1 Graph View (only visualization mode in MVP)

The graph uses a hierarchical layout: generations are arranged in horizontal rows with the oldest ancestors at the top and descendants at the bottom.

The entire tree is always rendered — there are no artificial generation windows or filters that hide nodes.

Navigation:
- Pan by dragging
- Zoom in and out via mouse wheel or pinch gesture (mobile)

On first load, the graph centers on the root person (the tree creator by default). The owner can designate a different default focus person for the tree.

#### 8.2 Person Node Card

Each node in the graph displays:
- Profile photo thumbnail
- Full name
- Birth year and death year (e.g. "1923 – 1987"), or birth year only for living persons (e.g. "1991 –")

Living and deceased persons are not visually distinguished on the node card.

Clicking a node opens the person drawer.

#### 8.3 Person Drawer

The person drawer slides in from the side of the screen. It is non-blocking — the graph remains interactive while the drawer is open.

The drawer is accessible via a shareable URL (`/tree/:treeId/person/:personId`). Opening this URL centers the graph on the referenced person and opens their drawer.

The drawer contains the following sections:

**Identity**
Name (all variants), dates, places, gender, nationality, religion, occupation.

**Relationships**
Two visual groups separated by a divider (no group labels):
- Group 1: Parents · Siblings
- Group 2: Spouse(s) / Ex-spouse(s) · Children

Each relative is shown as a mini-card. Clicking a mini-card re-centers the graph on that person and opens their drawer. All spouses and ex-spouses are shown; relationship status is implied by subtle visual treatment.

**Biography**
Public free text.

**Notes**
Private to the author. Never visible to any other user.

**Timeline**
Chronological list of key life events. See section 8.4.

#### 8.4 Timeline

The timeline is populated in two ways:

**Auto-derived from fact records:**
- Birth
- Marriage / partnership start
- Separation / divorce
- Death

**Manually added typed events** (each with optional date, place, description, and source citation):
- Military service
- Immigration / Emigration
- Education
- Career milestone
- Arrest / Imprisonment
- Religious event (baptism, confirmation, etc.)
- Other (free text type label)

All timeline events — both derived and manual — are stored as typed fact records.

#### 8.5 Color Themes

The owner can set a color theme for the tree (e.g. Classic, Dark, Sepia, Nature, Minimal). Any user can override the theme for themselves.

### 9. Search

#### 9.1 Cross-Tree Person Search

Any user, including guests without an account, can search for persons across all public trees by name.

Search result card shown to logged-in users:
- Profile photo
- Full name
- Birth / death years
- Birth / death location
- Tree name and owner display name

Search result card shown to guests:
- Full name
- Tree name and owner display name

Private trees are completely invisible to search regardless of the query.

#### 9.2 Search Filters

- First name and last name (separately or combined)
- Birth date or approximate birth year
- Death date or approximate death year
- Birth place
- Death place
- Date range (born between year X and year Y)
- Living status (living / deceased)
- Gender
- Tree name

#### 9.3 Search Within a Tree

The owner can search for persons within a specific tree they have access to. In-tree search is not subject to privacy restrictions.

### 10. Privacy Settings

Privacy settings control what non-collaborators can see when viewing a public tree. Collaborators always see all data regardless of privacy settings. A private tree is completely invisible to non-collaborators regardless of any field-level settings.

#### 10.1 Default Visibility

**Living persons:**
- Name fields (first name, last name, patronymic, matronymic) — visible
- Birth year — visible (shown in node card)
- Birth date (full) — hidden
- All other fields — hidden

**Deceased persons:**
- Name fields — visible
- All other fields — hidden by default

**Sensitive fields (hidden by default for all persons, regardless of living status):**
- Religion
- Nationality

#### 10.2 Configurable Fields

The owner can configure visibility per person for any of the following fields individually:
- First name, last name, patronymic, matronymic
- Birth date, birth place
- Death date, death place
- Biography
- Profile photo
- Nationality
- Religion
- Occupation

### 11. GEDCOM Export

The owner can export their tree as a GEDCOM file. The export includes all persons, relationships, fact records, and source citations. Media file references are included; actual media files are not bundled.

Export is available to the tree owner only.

---

## Post-MVP

### PM-1. Tree Sharing and Collaboration (ReBAC)

Access control follows Relationship-Based Access Control (ReBAC). The permission model is mixed: a tree-level default relation per collaborator, with subtree-level and person-level overrides. Resolution rule: most specific wins (person-level overrides subtree-level, subtree-level overrides tree-level).

The owner can:
- Invite collaborators by email address
- Set a tree-level default permission for each collaborator (viewer / editor / no access)
- Add subtree-level overrides (e.g. "editor access on the paternal branch only")
- Add person-level overrides (e.g. "block access to this specific person")
- Change a collaborator's permission level without revoking and re-inviting
- Revoke access for any collaborator at any time
- View the full list of current collaborators and their permission levels
- Generate a public link for view-only access and toggle it on or off
- Transfer tree ownership to any current editor

Editors cannot manage sharing, delete the tree, or transfer ownership.
Viewers cannot export the tree.

#### PM-1.1 Permission Levels

| Action                          | Owner | Editor | Viewer | Public link |
|---------------------------------|-------|--------|--------|-------------|
| View tree                       | yes   | yes    | yes    | yes         |
| Edit persons and relationships  | yes   | yes    | no     | no          |
| Delete persons                  | yes   | yes    | no     | no          |
| Export (GEDCOM, PDF)            | yes   | yes    | no     | no          |
| Manage sharing                  | yes   | no     | no     | no          |
| Delete tree                     | yes   | no     | no     | no          |
| Transfer ownership              | yes   | no     | no     | no          |

### PM-2. Privacy Settings UI

- Per-person field-level visibility configuration (fields defined in section 10.2)
- Bulk privacy action: "apply these settings to all living persons in this tree"

### PM-3. Notes — Shared Mode

- A note author can share a note with specific collaborators as read-only or editable
- Shared notes respect ReBAC — only explicitly granted users can see a shared note
- Notes support @mention: typing @username sends an immediate notification to the mentioned collaborator

### PM-4. Notifications

**In-app notification center** (bell icon) for all logged-in users.

High-value events — immediate in-app + email notification:
- Collaboration invite received
- Invite accepted or declined
- Tree access revoked
- Soft link proposed to your tree
- Hard merge requested

Medium-value events — in-app notification only:
- A collaborator added, edited, or deleted a person
- A collaborator added a source citation
- @mention in a shared note

### PM-5. Full Media Management

- Upload multiple photos per person (JPEG, PNG, WebP)
- Upload documents per person (PDF)
- Tree-level media gallery for media not linked to a specific person
- Media attachable to fact records (e.g. a photo of a marriage certificate attached to a marriage fact)
- Profile photo selectable from the person's uploaded photos

### PM-6. GEDCOM Import

- The owner can import a GEDCOM file into an existing or new tree
- Standard GEDCOM tags are mapped to the internal fact-based data model
- Potential duplicates are detected by matching name and birth date
- A conflict resolution step is presented before any data is saved
- Import is handled by an asynchronous background worker

### PM-7. Additional Visualization Modes

Implemented in the following priority order:

1. **Decorative view** — a graphical illustration styled as a real tree, with ancestors as roots and descendants as branches; intended for sharing and casual viewing; optimized for mobile display and PDF export
2. **Table view** — a sortable list of all persons with columns for name, birth date, and death date; useful for reviewing and editing large trees
3. **Fan chart view** — a radial chart showing ancestors expanding outward from a central person across generations

### PM-8. Audit Log and Change History

- A full audit log showing who changed which field, from what value to what value, and when
- Per-person change history visible in the person drawer
- The tree owner can revert any change made by a collaborator

### PM-9. Smart Matches

When a person in a user's tree closely matches a person in another public tree, the system surfaces the match as a suggested connection. The user reviews each suggestion and accepts or rejects it. Accepted matches feed into the soft link feature (PM-11).

### PM-10. Record Matching

When a person exists in a tree, the system searches connected genealogical archives (e.g. FamilySearch, national archives) for potentially matching historical records. Suggested records are shown for user review. Accepted records are attached as source citations on the relevant fact.

### PM-11. Tree Connecting (Soft Link)

- The owner can propose a soft link asserting that a person in their tree is the same individual as a person in another tree
- The two trees remain fully independent; the link is a navigable reference only
- The other tree's owner must accept or reject the proposal
- A soft link to a person in a private tree reveals only what that person's privacy settings permit

### PM-12. Tree Merging (Hard Merge)

- The owner can initiate a hard merge to import persons and relationships from a source tree into their own tree
- The system performs duplicate detection and presents a per-field conflict resolution interface before executing
- The user must confirm the merge explicitly twice before it is executed
- A merge log is stored
- This operation is irreversible

### PM-13. External Archive Integration

The system integrates with external genealogical archives (e.g. FamilySearch) via their REST API or by accepting GEDCOM and GedcomX files. All integrations are handled by asynchronous background workers so that large imports do not block the UI.

### PM-14. AI Features

- **Timeline event extraction:** AI-assisted extraction of structured life events from biography text, opt-in per person
- **AI photo scan:** the user uploads a photo of a paper family tree; the system analyses the structure and proposes a preliminary tree of persons and relationships for the user to review and confirm before saving
- **Natural language search:** the user describes a filter in plain language (e.g. "women born in Kyiv before 1900"); the system interprets the input and applies the corresponding search filters; if no confident matches exist, the result is empty

All AI features are billed via AI credits.

### PM-15. PDF Export and Printed Book

**PDF export:**
- Export the decorative view as a high-resolution PDF (decorative view first; graph view export in a later phase)
- Print options: paper size (A4, A3, Letter), orientation (portrait / landscape), generation depth, detail level (names only vs. full details), color theme

**Self-serve printed book (print-ready PDF):**
- Cover page with tree name
- One page per person: photo, biography, timeline
- Relationship diagrams per family unit
- Index

Platform-fulfilled physical print-on-demand is a later monetization phase.

### PM-16. Map Visualization

Birth and death places with stored geocoordinates can be visualized on a map within the person drawer.

### PM-17. Analytics

**Admin analytics (for the platform owner):**
Implemented via a third-party analytics tool (e.g. Posthog, Plausible). Covers: total registered users, growth over time, active users (DAU/MAU), trees created, persons added, media uploaded, feature usage breakdown, geographic distribution.

**User-facing tree analytics:**
- Tree completeness score (e.g. "42% of persons have a profile photo")
- Tree view count per week
- Most viewed persons
- Active collaborator count

### PM-18. Tree Discovery

Suggestions for "trees you might be connected to," based on matching surnames or birth places found in the user's own tree.

### PM-19. Monetization

- AI features are billed via AI credits (pay-per-use)
- Cloud storage is billed above a free tier threshold
- All other features are free
- A printed book is available as a one-time purchase

Note: monetization is not a current priority and is flagged for future consideration.

---

## Explicitly Excluded

The following are explicitly out of scope:

- DNA matching and DNA kit management
- Social feed / family timeline (Facebook-style activity stream)
- Internal messaging or family group chat
- Native mobile apps (iOS / Android)
- Bulk auto-import of suggested matches without user review ("instant discoveries")
- Platform-fulfilled physical book fulfillment (deferred to a later monetization phase)
