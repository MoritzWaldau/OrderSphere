# 0007 — Event-sourcing island: only the Order aggregate

**Status:** Accepted

## Context

Distributed systems benefit from an audit trail and temporal query capability on business-critical
aggregates. Full event sourcing across every service and aggregate introduces significant operational
complexity (event schema evolution, projection rebuilds, infrastructure dependencies on an event
store). A selective approach — an "island" of event sourcing within a predominantly state-based
system — captures the benefit where it matters most without the systemic cost.

The Order aggregate in the Ordering service is the highest-value candidate: order lifecycle events
are legally and operationally significant, refund/cancellation compensation requires replaying what
happened, and the aggregate's state transitions are strictly sequential.

## Decision

Only the **Order aggregate** is event-sourced. An append-only `order_events` table
(`StreamId`, `Version`, `EventType`, `Payload`, `OccurredAt`) is the system of record.
The composite primary key `(StreamId, Version)` provides optimistic concurrency — an insert
with a duplicate version fails immediately, preventing split-brain writes.

Events are defined as records with primitive properties (`Guid`, `string`, `int`, `decimal`) —
not value objects — so the stream contract is stable across domain model changes
(`Ordering.Domain/OrderEvents/`, implementing `IOrderEvent`).

A synchronous read projection (`OrderView`, `order_items`, `order_status_history`) is populated
in the same `SaveChangesAsync` call as the stream append, keeping reads fast without a separate
projection worker. Both the event store (`Ordering.Infrastructure/EventSourcing/OrderEventStore.cs`)
and the projection update execute in the same Postgres transaction, so they cannot diverge.

All other aggregates — Product, Cart, PaymentRecord, Coupon — remain state-based. This boundary
is deliberate and is not to be extended without a new ADR.

## Consequences

- Order history is immutable and auditable; compensating events can be applied without destructive
  updates.
- The event stream contract (field names, types) is a public commitment; breaking changes require
  a new event version or a migration of existing events.
- Projections are always in sync with the stream by construction (same transaction).
- Adding a new order lifecycle event requires: a new `IOrderEvent` record in `Domain/OrderEvents/`,
  an `Apply` branch in the aggregate, a projection update in `Infrastructure/EventSourcing/`, and
  an EF migration for any derived column or index change.
- Developers unfamiliar with event sourcing face a steeper ramp on the Order aggregate exclusively.
- Mixing event-sourced and state-based updates in the same aggregate is a violation of this decision.

## Alternatives considered

- **State-based Order** — rejected: no audit trail; compensation via status overwrite loses history.
- **Event sourcing for all aggregates** — rejected: high operational cost with diminishing return
  for simpler aggregates (Cart, Product).
- **Separate event store service** — rejected: unnecessary infrastructure for a single aggregate.
