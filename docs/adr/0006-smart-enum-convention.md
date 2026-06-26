# ADR 0006 — Smart-Enum Implementation Convention

- **Status:** Accepted
- **Date:** 2026-06-26
- **Deciders:** Product owner
- **Related:** ADR 0001 decision #6 (enum-as-string storage — this ADR refines *how* that is implemented), ADR 0002 (inline validation, RFC 9457), ADR 0003 (`LivingStatus`), ADR 0004 (`GenderType`, `CoupleType`, `ParentageType`), ADR 0005 (`ParentageType` on the re-architected Filiation). First implemented in #103 (`GenderType`).

## Context

ADR 0001 decision #6 fixed the *storage* convention for closed enums: persisted as **strings**, with a `CHECK (col IN (...))` constraint on the column. It assumed a plain C# `enum` mapped via `HasConversion<string>()`. Implementing the first such enum (`GenderType`, #103) exposed a gap that ADR 0001 did not resolve: **how an enum-typed value crosses the untrusted request boundary.**

A plain `int`-backed C# `enum`:

- serializes as a **number** by default (not the readable string the storage convention wants on the wire), and
- when an unknown string *is* accepted (via a global `JsonStringEnumConverter`), rejects it at **JSON deserialization** — producing a framework **400** in the model-binding shape, not the project's RFC 9457 `errorCode` ProblemDetails (ADR 0002).

The alternatives that "fix" this on a plain enum are each worse: a tolerant converter that maps unknown → an `Invalid` enum member reintroduces exactly the sentinel/double-state that ADR 0003 and ADR 0004 work to eliminate; a global string-enum converter is a cross-cutting change that flips every endpoint's enum contract and still yields 400s.

## Decision

**Closed enums that arrive from untrusted request input are implemented as "smart enums," not plain C# `enum`s.** A smart enum is a `sealed` class with:

- a **private constructor** and **string-valued static readonly instances** (the closed set);
- a `string Value` property (the wire/DB representation);
- a `static readonly IReadOnlyList<T> All` of the members;
- a `static T? TryParse(string?)` that returns the matching member, or **`null`** on an unrecognized value (and on `null` input);
- a type-scoped `JsonConverter<T?>` applied via `[JsonConverter(typeof(...))]` on the class, reading/writing `Value`.

This keeps the **exact** ADR 0001 storage contract — a bare **string** column guarded by the same `CHECK (col IN (...))` — while resolving the boundary problem:

- **Concrete DTO type.** Responses (and commands/entities) carry the smart-enum type, not a vague `string`.
- **422, not 400.** The **request DTO keeps the raw `string?`** (the untrusted edge). A per-feature `XParsing.Parse(string?) → ErrorOr<T?>` helper is called **in the endpoint before dispatch** (ADR 0002's guidance for parsing validators): `null`/absent → `null` (valid), a known value → the member, an unknown value → the feature's `XErrors.YInvalid` (`Error.Validation`) flowing through `MatchToResponse` to RFC 9457 **422**. The command/entity then carry the validated `T?`.
- **No sentinel in the domain.** The type holds only positively-asserted members. "Absent / unknown" is `null` (where the field is nullable); "rejected" lives in validation. There is **no** `Unknown`/`Invalid` member — preserving the no-double-state stance of ADR 0003 (living status) and ADR 0004 decision #3 (gender).
- **No global JSON change.** The converter is scoped to the type, so other endpoints' contracts are untouched (no global `JsonStringEnumConverter`).

EF maps it via `HasConversion(v => v.Value, s => T.TryParse(s)!)`; the `CHECK` constraint is the database backstop (ADR 0001 #6).

*Rejected:* (a) plain `int`-backed `enum` + `HasConversion<string>()` — number-on-wire and 400-on-unknown, as above; (b) tolerant converter mapping unknown → an `Invalid` member — revives the sentinel/double-state; (c) global `JsonStringEnumConverter` + typed request DTO — cross-cutting, and still 400 not 422.

## Consequences

- **Where ADR 0001 #6 and ADRs 0003/0004/0005 say "C# enum stored as string," read "smart enum per this ADR."** The value sets, nullability, and defaults those ADRs specify are unchanged; only the implementation type is refined.
- Each smart enum ships with: the sealed class + converter, a feature `XParsing.Parse` helper, an `Error.Validation` (`*.{Field}Invalid`) in the feature's `*Errors`, the `HasConversion` mapping, and the `CHECK` constraint.
- **Applies to:** `GenderType` (#103, done), `CoupleType` (#104), `ParentageType` (#102/#105 — the Filiation re-architecture), and `LivingStatus` (#96). `LivingStatus` is **derived, never request-bound** (ADR 0003), so it needs the sealed-class + string-serialization shape but **not** the `Parse`/`*Invalid`/`CHECK` machinery (nothing untrusted is parsed into it). `FactType` (ADR 0001) carries an `Other` member + `custom_label` and may keep a plain-enum shape if it is never bound directly from untrusted input; if it is, it follows this convention.
- The pattern is a small, reusable per-enum cost that amortizes across every closed set in the system.
