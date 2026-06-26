# ADR 0002 — Write/Read Handler Seam & Validation

- **Status:** Accepted
- **Date:** 2026-06-26
- **Deciders:** Product owner
- **Related:** requirements §1–§7; CONTEXT.md; ADR 0001 (facts/citations). Hardening phase — strengthening the Mini MVP (#58), not adding features.

## Context

The Mini MVP shipped three different shapes for "do work behind an endpoint":

- **Auth/Login** — `HandleAsync` (HTTP concerns) delegates to a private method returning `ErrorOr<T>` (logic). A real seam, but in-file.
- **Auth/Register** — endpoint injects `RegisterValidator` and calls `ValidateToErrorOrAsync` inline, then runs logic. `UserManager`-based; does **not** use the message bus.
- **Persons / Couples / Trees / Filiations** — all logic inline in `HandleAsync`: owner lookup, entity construction, `db.SaveChanges`, audit-field stamping by hand. No seam, no validation.

Two pieces of infrastructure already exist but are **not used by the HTTP write path**:

- **Wolverine** (`WolverineFx` 3.10) is wired (`UseWolverine`, `PersistMessagesWithPostgresql`, `AutoApplyTransactions`, a retry/dead-letter policy). Its only consumer is background jobs (`IJobDispatcher` → `bus.SendAsync`).
- **`AuditBehavior`** is registered as Wolverine middleware on `ICreateCommand`/`IUpdateCommand` messages — but **nothing implements those interfaces**, so it never runs for HTTP writes. Every endpoint stamps `CreatedBy/At`, `UpdatedBy/At` by hand. The "audit via pipeline behavior" non-negotiable is currently decorative.

`FluentValidation.AspNetCore` and `ErrorOr` are already referenced; validators are auto-registered (`AddValidatorsFromAssemblyContaining`).

This ADR records the seam for write and read endpoints and where input validation lives, so the inline-CRUD slices converge on one model and the existing audit middleware becomes real.

## Decisions

1. **Endpoints are thin HTTP adapters.** An endpoint parses the request, dispatches to a seam, maps the `ErrorOr<T>` result to an `IResult` via the existing `MatchToResponse` helper, and owns transport concerns only (status codes, `Created(...)` location, cookies). No business logic, no EF, no audit stamping in endpoints.

2. **Writes go through Wolverine commands, invoked in-process.** A write endpoint builds a command and awaits `bus.InvokeAsync<ErrorOr<TResponse>>(command)`. The Wolverine handler holds the logic + EF and runs synchronously in the request, inside the auto-applied transaction. *Rejected:* `SendAsync` (fire-and-forget through the outbox) for writes — it returns before the row exists and can't yield the created entity for a 201; `SendAsync`/outbox stays reserved for genuine background jobs (e.g. future notifications).

3. **Commands carry the audit fields; the existing `AuditBehavior` activates.** Write commands implement `ICreateCommand`/`IUpdateCommand`. The already-registered middleware stamps `CreatedBy/At` / `UpdatedBy/At` on the command before the handler runs. **The inline audit-stamping in every endpoint is deleted.** This makes the audit pipeline non-decorative for the first time. `AutoApplyTransactions` + the Postgres outbox come for free, which matters the moment a write must also enqueue a job in the same transaction.

4. **Reads use direct EF behind a per-slice static query type — never inline in the endpoint.** A read endpoint calls `XQuery.Handle(db, …, ct)` returning `ErrorOr<T>` and maps it. Direct EF inside; **no message bus for reads** (they need no audit, transaction, or outbox). *Rejected:* (a) reads inline in the endpoint — the complaint being fixed; (b) routing reads through Wolverine queries — bus indirection on trivial owner-scoped GETs that need nothing it provides; (c) an injected cross-slice `IQueries` service — becomes a horizontal grab-bag fighting the vertical-slice layout. The split is "commands through the bus, queries direct," not full CQRS.

5. **Validation is FluentValidation, invoked inline everywhere — no validation middleware.** Both command handlers and non-bus endpoints call the slice's validator at the top (the `ValidateToErrorOrAsync` shape `RegisterValidator` already uses), returning `ErrorOr.Validation` that flows through `MatchToResponse` → RFC 9457. Reads validate the same way: the static query method invokes its validator inline. *Rejected:* a Wolverine `ValidationBehavior` middleware (the "middleware everywhere" option). Although it would make validation unforgettable on command writes, it: (a) leaves three invocation styles unless Auth is also migrated; (b) to be truly uniform forces **Register onto the bus** — dragging token-exchange endpoints into transactions/outbox/retry they don't need, fragmenting Register's cookie-setting from its handler, and moving Register's async MX/HIBP I/O *inside* the command pipeline and its transaction; (c) makes validation invisible at the call site, weakening the locality the vertical-slice layout exists for. Inline validation extends the proven Auth pattern, adds no infrastructure, and keeps reads and writes validating identically.

6. **The error-code → `*Errors` mapping is extracted to a shared helper.** The per-validator `switch` that maps `ValidationFailure.ErrorCode` to an `Error.Validation(...)` (currently hand-rolled in `RegisterValidator.ValidateToErrorOrAsync`) becomes a shared base/extension so each slice's validator declares only rules, not mapping ceremony.

## Consequences

- The four inline CRUD slices (Person, Couple, Tree, Filiation) are restructured: endpoint → command → handler for writes, endpoint → static query for reads, with an inline validator on each. Audit stamping is removed from them and handled by `AuditBehavior`.
- The "audit via pipeline behavior" non-negotiable becomes true: commands flow through the middleware that was previously dead code.
- Validation runs inside the command handler, therefore inside the auto-applied transaction. For the CRUD validators (synchronous, cheap — required fields, closed-enum membership) this is fine. **Guidance:** if a future command needs *async I/O* validation (like Register's MX/HIBP), validate in the endpoint *before* dispatch to keep slow external calls out of the transaction.
- Wolverine keeps two roles, deliberately scoped: synchronous in-process command handling for HTTP writes (`InvokeAsync`) and fire-and-forget background jobs (`SendAsync`). The retry/dead-letter policy is intended for jobs; in-process command invocations surface errors to the caller as `ErrorOr` rather than being retried.
- This is the seam the relationships-API rework, living-status derivation, and the facts/citations slices (ADR 0001) all build on, so it is sequenced before them.
- Auth token-exchange endpoints (Login, Refresh, Logout) and Register stay as direct/inline endpoints — not migrated onto the bus. They work and are out of scope for this hardening pass.
