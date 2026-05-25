# Claude Code Instructions for OrderSphere

## Operating rules

- Audience for any document or explanation: enterprise architects. No marketing language, no slogans, no enthusiasm filler, no "not just X — it's Y" comparisons.
- Do not guess technical facts. If a concept, API, or library behavior isn't certain, research it before stating it.
- Documents are markdown. Diagrams are mermaid.
- State results and decisions directly.
- Use compact the session automaticlly when the context is > 100K tokens

## Architecture

Microservices over Clean Architecture with CQRS (MediatR) and DDD. Each service is independently deployable and owns its domain, persistence, and infrastructure. Errors flow through `Result<T>`, not exceptions. Entities carry audit fields and soft-delete via `AuditableEntity`.

Shared primitives live in `BuildingBlocks`:
- `BuildingBlocks.Domain` — `ICommand`, `IQuery`, `Result<T>`, `AuditableEntity`, `Error`, MediatR pipeline behaviors
- `BuildingBlocks.Contracts` — Integration event DTOs shared across service boundaries
- `BuildingBlocks.EventBus` — `IEventBus` abstraction
- `BuildingBlocks.EventBus.AzureServiceBus` — Azure Service Bus implementation

Layer dependency direction per service: `Api → Application → Infrastructure → Domain → BuildingBlocks.Domain`. No service references another service's projects directly — cross-service communication uses HTTP clients or Service Bus events.

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
| Catalog | `Catalog.Domain`, `Catalog.Application`, `Catalog.Infrastructure`, `Catalog.Api` | Product + Category CRUD; exposes gRPC internal endpoint; Redis hybrid caching |
| Ordering | `Ordering.Domain`, `Ordering.Infrastructure`, `Ordering.Api`, `Ordering.Worker` | Order lifecycle; checkout publishes to Service Bus; Worker creates orders and triggers payment |
| Basket | `Basket.Domain`, `Basket.Infrastructure`, `Basket.Api` | Customer cart; validates stock via `ICatalogClient` on add |
| Payment | `Payment.Domain`, `Payment.Infrastructure`, `Payment.Api`, `Payment.Worker` | Payment records; Worker consumes `payment-requests` queue |
| Webhooks | `Webhooks.Domain`, `Webhooks.Infrastructure`, `Webhooks.Api`, `Webhooks.Worker` | Webhook dispatch |
| Notification | `Notification.Worker` | Sends order confirmation emails via Azure Communication Services |
| UserProfile | `UserProfile.Domain`, `UserProfile.Infrastructure`, `UserProfile.Api` | Customer profile data |

### Infrastructure & Frontend
| Project | Responsibility |
|---|---|
| `src/OrderSphere.ServiceDefaults` | Shared startup: OpenTelemetry, health checks, service discovery, resilience |
| `src/OrderSphere.AppHost` | .NET Aspire orchestration (Postgres, Redis, Service Bus, all services) |
| `src/Gateways/OrderSphere.ApiGateway` | YARP reverse proxy — routes external traffic to services |
| `src/Gateways/OrderSphere.Bff` | BFF — hosts Blazor WASM, handles OIDC session |
| `src/OrderSphere.Web` | Blazor WASM client — pages, components, typed API clients |

### Tests
| Project | Covers |
|---|---|
| `tests/OrderSphere.Bff.Tests` | BFF integration tests |
| `tests/OrderSphere.Ordering.Authorization.Tests` | Ordering authorization policy tests |
| `tests/OrderSphere.RealmContract.Tests` | Keycloak realm contract tests |

## Conventions

These are the rules that are not derivable by reading existing code. For everything else, the existing handlers and configurations are the template — read one before writing a new one.

- Business validation returns a `Result<T>` failure. Exceptions are reserved for genuinely exceptional conditions (I/O failures, programmer errors).
- Commands and queries return `Result<TDto>`. DTOs are `record` types; entities are `class` types.
- New entities inherit `AuditableEntity`. Add a matching EF configuration in the service's `Infrastructure/EntityConfigurations/`.
- New features live in the owning service's `Features/<Aggregate>/<UseCase>/` folder (e.g. `Catalog.Application/Features/Products/CreateProduct/`). Each use case co-locates its command/query, handler, and validator.
- Cross-service calls go through typed HTTP client interfaces (e.g. `ICatalogClient`, `IBasketClient`). No direct project references across service boundaries.
- Integration events are defined in `BuildingBlocks.Contracts`. Publish via `IEventBus`; consume via `IIntegrationEventHandler<T>`.
- Queries against soft-deletable entities must filter `!x.IsDeleted`.
- All I/O is `async`/`await`. No `.Result`, no `.Wait()`, no `.GetAwaiter().GetResult()`.
- Nullable reference types are enabled. Treat warnings as real.

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

## UI and styling

All visual, theming, MudBlazor, and CSS rules live in `.github/copilot-instructions.md`. Do not duplicate them here. When making UI changes, read that file first.

## Commands

Run from the repository root (`E:\CSharp\OrderSphere`).

| Task | Command |
|---|---|
| Build | `dotnet build OrderSphere.slnx` |
| Run via Aspire | `dotnet run --project src/OrderSphere.AppHost` |
| Run BFF (with WASM) | `dotnet run --project src/Gateways/OrderSphere.Bff` |
| Tests | `dotnet test` |

### EF Migrations

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

- **Database**: PostgreSQL via EF Core. Each service has its own `DbContext` under `<Service>.Infrastructure/Persistence/`. Configurations applied via `ApplyConfigurationsFromAssembly`. Migrations are per-service (see Commands).
- **Cache**: Redis via .NET Hybrid Cache. Used by Catalog service for product/category reads.
- **Email**: Azure Communication Services. Implemented in `Notification.Worker/Email/NotificationEmailService.cs`. Triggered by `OrderPlacedIntegrationEvent`. Connection string and sender address read from configuration.
- **Service Bus**: Azure Service Bus via `BuildingBlocks.EventBus.AzureServiceBus`. Key queues/topics: `orders` (checkout → ordering), `payment-requests` (ordering → payment), `order-placed` (ordering → notification/webhooks).

## Ask before

- Adding a NuGet dependency.
- Schema changes that aren't trivially backward-compatible.
- Introducing a new architectural pattern or cross-cutting concern.
- Changing authentication or authorization flow.
- Breaking a public contract consumed by the UI client.

Proceed without asking for: bug fixes, refactors inside one layer, new features that follow an existing feature pattern, UI changes consistent with `copilot-instructions.md`, performance improvements with no behavior change.

## Commit format

Conventional commits: `feat:`, `fix:`, `refactor:`, `docs:`, `style:`, `test:`, `chore:`. One-line subject describing the user-visible change; body for rationale if non-obvious.


# graphify
- **graphify** (`~/.claude/skills/graphify/SKILL.md`) - any input to knowledge graph. Trigger: `/graphify`
When the user types `/graphify`, invoke the Skill tool with `skill: "graphify"` before doing anything else.
