# 0011 — Invoicing: in-Api Service Bus consumer without an outbox

**Status:** Accepted

## Context

Ordering and Payment run their Service Bus consumers in dedicated `.Worker` projects and publish
integration events through a transactional outbox, so the event publish commits in the same database
transaction as the state change. Invoicing has a simpler shape: it consumes one queue
(`invoice-generation`, carrying `OrderPlacedIntegrationEvent`), generates a PDF, persists one
`Invoice`, and publishes exactly one follow-up event (`invoice-ready`). It also exposes
JWT-protected download endpoints.

Two questions followed: where should the consumer run, and does the single follow-up publish need an
outbox?

## Decision

1. **Consumer in the Api.** `InvoiceProcessor` is a `BackgroundService` hosted inside
   `Invoicing.Api`, which also serves the download endpoints. No separate `.Worker` project is
   created — one process owns both the event handling and the read surface.

2. **No outbox on the publish.** `InvoiceProcessor` publishes `invoice-ready` directly via
   `IEventBus`. Correctness rests on at-least-once delivery plus idempotent consumers: inbox dedupe
   on the inbound side prevents reprocessing, and the downstream `InvoiceGeneratedProcessor` dedupes
   on the event `Id`. A failure after persistence abandons the message; redelivery is made
   idempotent by the inbox check.

## Consequences

- Fewer moving parts for a service whose workload is a single linear consume-generate-publish step.
- The dual-write window (persist invoice, then publish) is tolerated because both ends are
  idempotent; this is acceptable only for this single-publish, idempotent-consumer shape. A service
  that publishes multiple events or needs strict publish-on-commit guarantees must use the outbox
  pattern instead.
- The decision is service-local and does not change the Ordering/Payment outbox approach (see
  [0008](0008-saga-choreography-service-bus.md)).

## Alternatives considered

- **Separate `.Worker` project + transactional outbox** (as Ordering/Payment) — rejected as
  over-engineered for one queue and one follow-up event; the idempotency guarantees already in place
  make the outbox redundant here.
