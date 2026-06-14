# Glossary — Ubiquitous Language

The shared vocabulary of the OrderSphere domain. Terms are used consistently in code, events, and
documentation. Aggregate and feature details are in
[architecture.md](architecture.md#features); the identity terms are expanded in
[auth/role-model.md](auth/role-model.md).

## Domain aggregates

| Term | Meaning |
|---|---|
| **Product** | A sellable catalog item with price and stock, owned by the **Catalog** service. Looked up by id or public slug. |
| **Category** | A hierarchy node in the catalog; products reference a category by id. Owned by **Catalog**. |
| **Basket** / **Cart** | A customer's current selection of items, owned by the **Basket** service. "Basket" is the service/aggregate name; "Cart" is the same concept in UI and HTTP routes (`/cart`). Adding an item validates stock against Catalog. |
| **Order** | A placed purchase, owned by the **Ordering** service. Moves through statuses Created → Paid → Shipped → Delivered (or Cancelled). |
| **Checkout** | The cart-to-order operation: fetches the cart, decrements stock synchronously, then publishes `CheckoutCartIntegrationEvent`. A process, not a stored aggregate. |
| **Coupon** | A discount code validated by the Ordering `ValidateCoupon` query (currently hardcoded codes). |
| **PaymentRecord** | A payment for an order, owned by the **Payment** service. Statuses: Pending → Authorized → Captured (or Failed). Created by the Payment worker from the `payment-requests` queue. |
| **Webhook subscription** | A registered outbound HTTP endpoint owned by the **Webhooks** service; dispatched to when matching integration events occur. |
| **CustomerProfile** | A customer's profile and addresses, owned by the **UserProfile** service. Keyed by the raw Auth0 `sub` (`Subject`). |
| **Conversation** | A durable advisory chat session owned by the **Advisory** service, keyed by `(CustomerSub, ConversationKey)`. |

## Identity

| Term | Meaning |
|---|---|
| **`sub`** | The Auth0 subject claim (`auth0\|<opaque_id>`) — the single source of caller identity. |
| **`CustomerId`** | A deterministic RFC 4122 v5 GUID derived from `sub` (`CustomerId.FromSub`), used as the key in Order, Basket, and Webhook aggregates. See [ADR 0002](adr/0002-customerid-deterministic-guid-from-sub.md). |
| **`Subject`** | The raw `sub` string stored verbatim by UserProfile as its lookup key. |
| **Role** | An Auth0 RBAC role (e.g. `customer`, `csr`, `order-manager`, `catalog-admin`, `admin`) emitted into the `https://ordersphere.dev/roles` claim. See [auth/role-model.md](auth/role-model.md). |

## Messaging and patterns

| Term | Meaning |
|---|---|
| **Integration event** | A JSON message published across service boundaries over Azure Service Bus, defined in `BuildingBlocks.Contracts`. Naming and rules: [contracts/CONVENTIONS.md](../contracts/CONVENTIONS.md). |
| **Outbox** | A service's transactional outbox: domain changes and the events they emit are persisted atomically, then dispatched to Service Bus by an outbox handler. |
| **Inbox** | The consumer-side idempotency store that records processed message ids so a redelivered message is not handled twice. |
| **`Result<T>`** | The success-or-`Error` return type for handlers and clients, used instead of exceptions for expected outcomes. See [ADR 0001](adr/0001-result-type-over-exceptions.md). |
| **`AuditableEntity`** | The base entity carrying audit fields and `IsDeleted`; soft-deleted via a global query filter. See [ADR 0006](adr/0006-soft-delete-global-query-filter.md). |

## Edge and AI

| Term | Meaning |
|---|---|
| **BFF** | Backend-for-Frontend (`OrderSphere.Bff`): hosts the WASM client, owns the OIDC session/cookie, proxies API calls. See [ADR 0003](adr/0003-bff-and-api-gateway.md). |
| **API gateway** | The YARP reverse proxy (`OrderSphere.ApiGateway`): the single external API ingress routing to services. |
| **Advisory agent** | The Azure OpenAI / Foundry-backed chat service (`Advisory.Api`) that answers customer questions; owns no tools and is inert without the MCP server. |
| **MCP server** | `Mcp.Server`: a Model Context Protocol tool server exposing the read-only `/api/v1` Gateway surface as user-scoped tools, forwarding the caller's bearer token. |
