# 0006 — Soft-delete via a global EF query filter

**Status:** Accepted

## Context

Entities require auditability and recoverable deletion rather than physical row removal. If
soft-delete is enforced per query (`Where(x => !x.IsDeleted)` in every handler), a single forgotten
filter leaks deleted rows — a correctness and potential data-exposure bug that is easy to introduce
and hard to spot in review.

## Decision

Every `AuditableEntity` carries `IsDeleted` (and audit fields) and gets
`builder.HasQueryFilter(x => !x.IsDeleted)` in its EF entity configuration. The filter is applied
once at the model level, so all queries inherit it automatically; handlers never repeat `!x.IsDeleted`.
Reading deleted rows is the deliberate exception, done explicitly with `IgnoreQueryFilters()`.

## Consequences

- Soft-delete is correct by default; a new query cannot accidentally include deleted rows.
- Each new `AuditableEntity` must add the query filter in its configuration — a fixed, mechanical step
  enforced by convention (see [../../CLAUDE.md](../../CLAUDE.md)).
- Deliberate access to deleted data is visible and greppable (`IgnoreQueryFilters`), not the default.
- Query filters interact with required relationships and `Include`; navigation targets should carry a
  consistent filter to avoid surprising results.

## Alternatives considered

- **Per-query `!IsDeleted`** — rejected: one omission leaks deleted rows.
- **Physical deletes** — rejected: no audit trail, no recovery.
