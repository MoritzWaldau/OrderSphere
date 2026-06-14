# 0004 — Database-per-service on PostgreSQL

**Status:** Accepted

## Context

Each service owns a distinct domain (catalog, basket, ordering, payment, user profile, webhooks,
notification, advisory). Sharing one schema couples services through the database, lets one service's
migrations break another, and undermines independent deployability.

## Decision

Every service owns its own PostgreSQL database and `DbContext` (under
`<Service>.Infrastructure/Persistence/`), reached through an `I<Service>DbContext` interface declared
in `<Service>.Application/Abstractions/`. Migrations are per-service (see the migration matrix in
[../architecture.md](../architecture.md#ef-migrations)). No cross-database joins and no
foreign keys across service boundaries; cross-service data flows through HTTP clients or integration
events ([0005](0005-http-clients-and-events-not-grpc.md)).

## Consequences

- Services are independently deployable and migrate their own schema on startup
  (`Database.Migrate()`), with no shared-schema coordination.
- Data needed from another service is fetched over HTTP or arrives as an event; there is no joining
  across stores. Identity is correlated by deterministic derivation, not foreign keys
  ([0002](0002-customerid-deterministic-guid-from-sub.md)).
- Consistency across services is eventual, achieved through the outbox/inbox pattern.
- More databases to provision and operate; the Aspire AppHost manages this for local and cloud runs.

## Alternatives considered

- **Single shared database** — rejected: couples deploys and schemas, breaks service ownership.
- **Schema-per-service in one database** — rejected: still a shared failure and migration domain.
