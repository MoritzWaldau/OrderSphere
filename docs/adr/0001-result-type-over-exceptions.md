# 0001 — `Result<T>` over exceptions for business outcomes

**Status:** Accepted

## Context

Command and query handlers must signal both success and *expected* failure (validation errors, not
found, conflict, forbidden). Modelling expected failures as exceptions couples control flow to
`try/catch`, obscures the set of outcomes a handler can produce, and carries a runtime cost on a path
that is not exceptional.

## Decision

Business outcomes flow through `Result` / `Result<T>` (`BuildingBlocks.Domain`). Handlers return
`Result<TDto>`; failures carry an `Error` value. Exceptions are reserved for genuinely exceptional
conditions — I/O failures, programmer errors, infrastructure faults. Typed cross-service HTTP clients
follow the same contract and return `Result<T>` rather than throwing on a handled failure.

## Consequences

- The outcome set of a handler is explicit in its signature; callers must handle failure to read the
  value.
- A MediatR pipeline and endpoint mapping translate `Error` to the appropriate HTTP status uniformly.
- Mixing exceptions and results is a smell: anything a caller is expected to handle belongs in the
  `Result`, not in a thrown exception.
- New handlers must adopt the pattern; see existing handlers as the template.

## Alternatives considered

- **Exceptions for validation/flow** — rejected: hidden control flow, weaker signatures, cost.
- **Nullable returns / sentinel values** — rejected: cannot carry error detail.
