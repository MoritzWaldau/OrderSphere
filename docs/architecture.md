# OrderSphere — Architecture Reference

Detailed system map for OrderSphere. Behavioral rules and conventions live in the
repository-root [CLAUDE.md](../CLAUDE.md); this file is the lookup reference it points to.

## Shared primitives (BuildingBlocks)

- `BuildingBlocks.Domain` — `ICommand`, `IQuery`, `Result<T>`, `AuditableEntity`, `Error`, MediatR pipeline behaviors
- `BuildingBlocks.Contracts` — Integration event DTOs shared across service boundaries
- `BuildingBlocks.EventBus` — `IEventBus` abstraction
- `BuildingBlocks.EventBus.AzureServiceBus` — Azure Service Bus implementation

## Project layout

### BuildingBlocks
| Project | Responsibility |
|---|---|
| `src/BuildingBlocks/OrderSphere.BuildingBlocks.Domain` | `ICommand`, `IQuery`, `Result<T>`, `AuditableEntity`, `Error`, MediatR behaviors |
| `src/BuildingBlocks/OrderSphere.BuildingBlocks.Contracts` | Integration event DTOs (`CheckoutCartIntegrationEvent`, `OrderPlacedIntegrationEvent`, etc.) |
| `src/BuildingBlocks/OrderSphere.BuildingBlocks.EventBus` | `IEventBus` abstraction |
| `src/BuildingBlocks/OrderSphere.BuildingBlocks.EventBus.AzureServiceBus` | Azure Service Bus implementation |

### Services
| Service | Projects | Notes |
|---|---|---|
| Catalog | `Catalog.Domain`, `Catalog.Application`, `Catalog.Infrastructure`, `Catalog.Api` | Product + Category CRUD; Redis hybrid caching |
| Ordering | `Ordering.Domain`, `Ordering.Application`, `Ordering.Infrastructure`, `Ordering.Api`, `Ordering.Worker` | Order lifecycle; checkout publishes to Service Bus; Worker creates orders and triggers payment |
| Basket | `Basket.Domain`, `Basket.Application`, `Basket.Infrastructure`, `Basket.Api` | Customer cart; validates stock via `ICatalogClient` on add |
| Payment | `Payment.Domain`, `Payment.Application`, `Payment.Infrastructure`, `Payment.Api`, `Payment.Worker` | Payment records; Worker consumes `payment-requests` queue |
| Webhooks | `Webhooks.Domain`, `Webhooks.Infrastructure`, `Webhooks.Api`, `Webhooks.Worker` | Webhook dispatch; no Application layer (event ingestion only) |
| Notification | `Notification.Worker` | Sends order confirmation emails via Azure Communication Services |
| UserProfile | `UserProfile.Domain`, `UserProfile.Application`, `UserProfile.Infrastructure`, `UserProfile.Api` | Customer profile data |
| Advisory | `Advisory.Api`, `Mcp.Server` (both under `src/Services/Advisory/`) | Customer-advisory AI agent + MCP tool server. See [AI advisory](#ai-advisory-agent--mcp-server). |

### Infrastructure & Frontend
| Project | Responsibility |
|---|---|
| `src/OrderSphere.ServiceDefaults` | Shared startup: OpenTelemetry, health checks, service discovery, resilience |
| `src/OrderSphere.AppHost` | .NET Aspire orchestration (Postgres, Redis, Service Bus, all services) |
| `src/Gateways/OrderSphere.ApiGateway` | YARP reverse proxy — routes external traffic to services |
| `src/Gateways/OrderSphere.Bff` | BFF — hosts Blazor WASM, handles OIDC session |
| `src/OrderSphere.Web` | Blazor WASM client — pages, components, typed API clients |

### Tests
xUnit + FluentAssertions (NSubstitute for mocking, EF Core InMemory where a `DbContext` is needed).

| Project | Covers |
|---|---|
| `tests/OrderSphere.Domain.Tests` | Domain entity/value-object unit tests across Ordering, Basket, Catalog, Payment |
| `tests/OrderSphere.Ordering.Checkout.Tests` | Ordering checkout flow |
| `tests/OrderSphere.Ordering.Authorization.Tests` | Ordering authorization policy tests |
| `tests/OrderSphere.UserProfile.Tests` | UserProfile service |
| `tests/OrderSphere.Bff.Tests` | BFF integration tests |
| `tests/OrderSphere.RealmContract.Tests` | Keycloak realm contract tests |
| `tests/OrderSphere.Mcp.Tests` | MCP tool methods against a mocked gateway (NSubstitute) |

## Features

| Aggregate | Service | Notes |
|---|---|---|
| Cart | Basket | Add/remove/decrease items; validates stock via Catalog HTTP client |
| Product | Catalog | CRUD + public slug/id lookups; Redis hybrid caching on reads |
| Category | Catalog | Hierarchy management; products reference categories by ID |
| Order | Ordering | Placement, retrieval, status transitions (Created → Paid → Shipped → Delivered / Cancelled) |
| Checkout | Ordering | Cart-to-order flow: fetches cart + decrements stock synchronously, then publishes `CheckoutCartIntegrationEvent` to Service Bus |
| Coupon | Ordering | `ValidateCoupon` query; hardcoded codes `WELCOME10`, `SUMMER15` |
| Payment | Payment | `PaymentRecord` created by Worker on `payment-requests` queue; status: Pending → Authorized → Captured / Failed |
| Webhooks | Webhooks | Outbound webhook dispatch triggered by integration events |
| Notification | Notification | Order confirmation email on `OrderPlacedIntegrationEvent` |
| UserProfile | UserProfile | Customer profile data |

## AI advisory agent + MCP server

A customer-advisory chat agent, deliberately split into two independently deployable
components under `src/Services/Advisory/`:

- **`OrderSphere.Mcp.Server`** — a Model Context Protocol server (Streamable HTTP, `MapMcp("/mcp")`).
  Holds **no** LLM logic, only tools. Each tool wraps the public API Gateway surface through a typed
  `IOrderSphereGateway` (`Gateway/OrderSphereGateway.cs`). Reusable by the internal agent and by
  external MCP clients (Claude Desktop, IDEs).
- **`OrderSphere.Advisory.Api`** — the agent service (layered: `Advisory.Domain`, `Advisory.Application`,
  `Advisory.Infrastructure`, `Advisory.Api`). Connects to Azure OpenAI / Foundry
  (`DefaultAzureCredential`, no API key) via Microsoft Agent Framework and exposes a streaming chat
  endpoint (`POST /chat`, SSE) plus read-only history (`GET /conversations`,
  `GET /conversations/{id}`). It owns **no** tools of its own: it loads the MCP server's tools per
  request (`AdvisorChatService`) and is inert without the MCP connection. `/chat` is rate-limited
  **per user** (partitioned by `sub`) because each request drives an LLM completion.

### Conversation persistence

Conversations are durable, owned per customer (Keycloak `sub`), in `advisory-db` (Postgres, EF Core).
After each turn `AdvisorChatService` serializes the agent session (chat history) via
`AIAgent.SerializeSessionAsync` into `Conversation.SerializedSession` and rehydrates it on the next
request with `DeserializeSessionAsync` — so context survives restarts and is shared across instances.
A human-readable transcript is stored in `ConversationMessages` (role + text) for display and audit.
The `(CustomerSub, ConversationKey)` pair is unique. The DbContext is reached through
`IAdvisoryDbContext` (declared in `Advisory.Application/Abstractions`); the chat service is scoped.

The agent is built per request because its tools are bound to the **current user**: the MCP client
carries the caller's bearer token so user-scoped tools resolve the correct customer.

### Identity forwarding

Data stays customer-scoped through a bearer-token chain:
`BFF (cookie → access_token) → Advisory.Api → MCP.Server → API Gateway → services`.
The MCP server attaches the inbound `Authorization` header to downstream calls via
`Gateway/BearerForwardingHandler.cs`; existing JWT validation on each service enforces scoping.
Public catalog tools work anonymously; user-scoped tools return no data without a valid token.

### MCP tools

All read-only. Routes are the public `/api/v1` Gateway surface.

| Tool | Route | Scope |
|---|---|---|
| `search_products` | `GET /products` | public |
| `get_product` | `GET /products/{slug}` | public |
| `list_categories` | `GET /categories` | public |
| `get_my_orders` | `GET /orders` | user |
| `get_order_status` | `GET /orders/{id}` | user |
| `validate_coupon` | `GET /coupons/validate` | user |
| `get_my_profile` | `GET /profile` | user |
| `list_my_addresses` | `GET /profile/addresses` | user |
| `get_my_cart` | `GET /cart` | user |
| `get_payment_status` | `GET /payments/by-order/{orderId}` | user |

Write actions (add-to-cart, checkout, profile mutations) are intentionally excluded; adding them is a
new auth/workflow path that requires explicit sign-off.

### Configuration

`Foundry:Endpoint` and `Foundry:Deployment` are read by `Advisory.Api`. When the endpoint is unset the
agent degrades gracefully (returns a "not configured" message) so local runs work without Azure. Set
locally via user-secrets on the AppHost; authenticate with `az login`. The Keycloak realm defines a
public PKCE client `advisory-mcp` for external MCP clients.

## EF Migrations

Each service owns its migrations. Pattern: `-p <Infrastructure project> -s <Api project>`.

| Service | Add migration | Apply |
|---|---|---|
| Catalog | `dotnet ef migrations add <Name> -p src/Services/Catalog/OrderSphere.Catalog.Infrastructure -s src/Services/Catalog/OrderSphere.Catalog.Api` | same with `database update` |
| Ordering | `dotnet ef migrations add <Name> -p src/Services/Ordering/OrderSphere.Ordering.Infrastructure -s src/Services/Ordering/OrderSphere.Ordering.Api` | same with `database update` |
| Basket | `dotnet ef migrations add <Name> -p src/Services/Basket/OrderSphere.Basket.Infrastructure -s src/Services/Basket/OrderSphere.Basket.Api` | same with `database update` |
| Payment | `dotnet ef migrations add <Name> -p src/Services/Payment/OrderSphere.Payment.Infrastructure -s src/Services/Payment/OrderSphere.Payment.Api` | same with `database update` |
| Webhooks | `dotnet ef migrations add <Name> -p src/Services/Webhooks/OrderSphere.Webhooks.Infrastructure -s src/Services/Webhooks/OrderSphere.Webhooks.Api` | same with `database update` |
| UserProfile | `dotnet ef migrations add <Name> -p src/Services/UserProfile/OrderSphere.UserProfile.Infrastructure -s src/Services/UserProfile/OrderSphere.UserProfile.Api` | same with `database update` |
| Advisory | `dotnet ef migrations add <Name> -p src/Services/Advisory/OrderSphere.Advisory.Infrastructure -s src/Services/Advisory/OrderSphere.Advisory.Api` | same with `database update` |

## External services

- **Database**: PostgreSQL via EF Core. Each service has its own `DbContext` under `<Service>.Infrastructure/Persistence/`. Configurations applied via `ApplyConfigurationsFromAssembly`. Migrations are per-service (see above).
- **Cache**: Redis via .NET Hybrid Cache. Used by Catalog service for product/category reads.
- **Email**: Azure Communication Services. Implemented in `Notification.Worker/Email/NotificationEmailService.cs`. Triggered by `OrderPlacedIntegrationEvent`. Connection string and sender address read from configuration.
- **Service Bus**: Azure Service Bus via `BuildingBlocks.EventBus.AzureServiceBus`. Key queues/topics: `orders` (checkout → ordering), `payment-requests` (ordering → payment), `order-placed` (ordering → notification/webhooks).

## Payment provider integration

### Architecture

Payment processing is abstracted behind `IPaymentProvider` (in `Payment.Infrastructure/Providers/`).
The interface models a two-phase flow: `AuthorizeAsync → CaptureAsync → RefundAsync`, all returning
`Result<T>`. `PaymentProviderFactory` resolves the correct provider by `MethodName` (case-insensitive).

The current providers (`CreditCardPaymentProvider`, `PayPalPaymentProvider`, `InvoicePaymentProvider`)
are simulated placeholders. `CreditCardPaymentProvider` is the designated slot for a future Stripe
integration.

### Adding a new provider (e.g. Stripe)

1. **Create** `XyzPaymentProvider : IPaymentProvider` in `Payment.Infrastructure/Providers/`.
   - `MethodName` must match the string sent in `PaymentRequestedIntegrationEvent.PaymentMethod`.
   - `AuthorizeAsync` creates the charge intent; `CaptureAsync` finalises it; `RefundAsync` reverses it.
   - Use `IOptions<PaymentOptions>` (or a dedicated sub-options class) for API keys and secrets.
     Never hardcode credentials — inject via User Secrets locally and Key Vault in production.
2. **Register** in `DependencyInjection.AddPaymentInfrastructure`:
   `services.AddSingleton<IPaymentProvider, XyzPaymentProvider>();`
   Remove or retain the placeholder provider as appropriate.
3. **Idempotency**: `PaymentRequest.OrderId` is suitable as an idempotency key for providers that
   support it (e.g. Stripe's `IdempotencyKey` header). Use it to prevent double-charges on retries.
4. **Webhook-based capture** (asynchronous confirmation) is not yet modelled. The current
   `Authorize → Capture` sequence is synchronous and suitable for providers with manual capture.
   Asynchronous capture confirmation requires a separate webhook endpoint and a new domain transition
   on `PaymentRecord` — treat that as a separate feature.

### Development bypass

When `Payment:BypassProviders` is `true` (default in `appsettings.Development.json`), no provider
is contacted. The `PaymentProcessor` marks every payment as `Captured` immediately with a `DEV-*`
transaction ID. Set to `false` locally to exercise the real provider path.
