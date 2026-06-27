# 0008 — Saga choreography over Service Bus for the order/payment flow

**Status:** Accepted

## Context

Completing an order requires coordinated state changes across the Ordering and Payment services.
These services cannot share a transaction. Two approaches exist: a central **orchestrator** that
drives each step and handles failures, or **choreography** where services react to events on a
shared message bus and each service owns its own compensation logic.

## Decision

The order/payment flow uses **choreography-based saga** over Azure Service Bus. No central
orchestrator exists. The sequence is:

1. `OrderProcessor` worker creates the order and publishes a payment-request message to the
   `payment-requests` queue.
2. The Payment service processes the request and publishes the result to `payment-results`.
3. `PaymentResultProcessor` worker consumes the result and either confirms the order or
   initiates compensation by publishing a refund request to `payment-refunds`.
4. `PaymentRefundProcessor` worker finalises the compensation path.

Saga state is projected into the `order_sagas` table (`Ordering.Domain/Entities/OrderSaga.cs`),
keyed by the Service Bus `CorrelationId`. The projection is updated atomically within each
worker's `SaveChangesAsync` call, so the saga table is always queryable and consistent with
the Ordering service's view of the flow.

The Inbox pattern (`EfInboxStore`) provides idempotent consumption on all queues — duplicate
deliveries are deduplicated before any state change.

## Consequences

- Each service remains independently deployable; there is no orchestrator service to maintain.
- Adding a new step to the order flow requires a new queue, a new worker consumer, and updating
  the `OrderSaga` state machine — not a central orchestration definition.
- Debugging a failed saga requires correlating messages across queues using `CorrelationId`.
  The `order_sagas` table provides a queryable snapshot of flow state per order.
- Compensation (refund path) is implemented as first-class workers, not exception handlers.
  A saga in `CompensationPending` state triggers the refund worker; `Refunded` is terminal.
- The Inbox deduplication window must exceed the Service Bus `MaxDeliveryCount` × `LockDuration`
  to prevent double processing under retry pressure.

## Alternatives considered

- **MassTransit State Machine (orchestrator)** — rejected: adds a framework dependency and an
  additional coordination point without a meaningful simplification given two services and one
  payment step.
- **Two-phase commit / distributed transaction** — rejected: not supported across independent
  services with separate databases.
- **Saga orchestrator as a dedicated service** — rejected: overkill for a two-service flow;
  complexity scales with number of services, not total steps.
