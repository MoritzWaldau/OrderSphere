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

## External services

- **Database**: PostgreSQL via EF Core. Each service has its own `DbContext` under `<Service>.Infrastructure/Persistence/`. Configurations applied via `ApplyConfigurationsFromAssembly`. Migrations are per-service (see above).
- **Cache**: Redis via .NET Hybrid Cache. Used by Catalog service for product/category reads.
- **Email**: Azure Communication Services. Implemented in `Notification.Worker/Email/NotificationEmailService.cs`. Triggered by `OrderPlacedIntegrationEvent`. Connection string and sender address read from configuration.
- **Service Bus**: Azure Service Bus via `BuildingBlocks.EventBus.AzureServiceBus`. Key queues/topics: `orders` (checkout → ordering), `payment-requests` (ordering → payment), `order-placed` (ordering → notification/webhooks).
