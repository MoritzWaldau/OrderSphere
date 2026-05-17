# Claude Code Instructions for OrderSphere

## Operating rules

- Audience for any document or explanation: enterprise architects. No marketing language, no slogans, no enthusiasm filler, no "not just X — it's Y" comparisons.
- Do not guess technical facts. If a concept, API, or library behavior isn't certain, research it before stating it.
- Documents are markdown. Diagrams are mermaid.
- State results and decisions directly.

## Architecture

Clean Architecture with CQRS (MediatR) over a DDD domain model. Errors flow through `Result<T>`, not exceptions. Entities carry audit fields and soft-delete via `AuditableEntity`. Domain events implement MediatR `INotification`. Order processing is asynchronous via Azure Service Bus.

Layer dependency direction: `UI → Application → Infrastructure → Domain`. Domain has no outward dependencies.

## Project layout

| Project | Responsibility |
|---|---|
| `src/OrderSphere.Domain` | Entities, value objects, domain events, errors, `Result<T>`, CQRS abstractions (`ICommand`, `IQuery`, `ICommandHandler`, `IQueryHandler`) |
| `src/OrderSphere.Application` | Feature handlers under `Features/<Aggregate>/<UseCase>/`, DTOs, validators, MediatR registration |
| `src/OrderSphere.Infrastructure` | EF Core (`Persistence/`), entity configurations (`EntityConfigurations/`), Azure email (`Email/`), Azure Service Bus (`ServiceBus/`), interceptors |
| `src/OrderSphere.UI/OrderSphere.UI` | Blazor Server host, pages, layouts, DI wiring — startup project |
| `src/OrderSphere.UI/OrderSphere.UI.Client` | Client-side interactive components |
| `src/OrderSphere.AppHost` | .NET Aspire orchestration (Postgres, Service Bus, app) |
| `tests/OrderSphere.Domain.Tests` | Domain unit tests |
| `tests/OrderSphere.Application.Tests` | Application/handler tests |

## Conventions

These are the rules that are not derivable by reading existing code. For everything else, the existing handlers and configurations are the template — read one before writing a new one.

- Business validation returns a `Result<T>` failure. Exceptions are reserved for genuinely exceptional conditions (I/O failures, programmer errors).
- Commands and queries return `Result<TDto>`. DTOs are `record` types; entities are `class` types.
- New entities inherit `AuditableEntity`. Add a matching configuration in `src/OrderSphere.Infrastructure/EntityConfigurations/`.
- New features live in `src/OrderSphere.Application/Features/<Aggregate>/<UseCase>/`. Each use case co-locates its command/query, handler, and validator.
- Queries against soft-deletable entities must filter `!x.IsDeleted`.
- All I/O is `async`/`await`. No `.Result`, no `.Wait()`, no `.GetAwaiter().GetResult()`.
- Nullable reference types are enabled. Treat warnings as real.

## Features

| Aggregate | Notes |
|---|---|
| Cart | Customer cart, add/remove/update items |
| Order | Order placement, retrieval, status transitions |
| Product | Catalog product CRUD and lookups (by id, by slug) |
| Category | Product category hierarchy |
| Checkout | Cart-to-order checkout flow, publishes `CheckoutCartEvent` to Service Bus |
| Coupon | Coupon validation (`ValidateCoupon` query); errors in `Domain/Errors/CouponErrors.cs` |
| Admin | Administrative operations |

## UI and styling

All visual, theming, MudBlazor, and CSS rules live in `.github/copilot-instructions.md`. Do not duplicate them here. When making UI changes, read that file first.

## Commands

Run from the repository root (`E:\CSharp\OrderSphere`).

| Task | Command |
|---|---|
| Build | `dotnet build OrderSphere.slnx` |
| Run via Aspire | `dotnet run --project src/OrderSphere.AppHost` |
| Run UI directly | `dotnet run --project src/OrderSphere.UI/OrderSphere.UI` |
| UI hot reload | `dotnet watch --project src/OrderSphere.UI/OrderSphere.UI` |
| Tests | `dotnet test` |
| Add EF migration | `dotnet ef migrations add <Name> -p src/OrderSphere.Infrastructure -s src/OrderSphere.UI/OrderSphere.UI` |
| Apply EF migrations | `dotnet ef database update -p src/OrderSphere.Infrastructure -s src/OrderSphere.UI/OrderSphere.UI` |

Startup project for EF tooling is `src/OrderSphere.UI/OrderSphere.UI`, not `src/OrderSphere.UI`.

## External services

- **Database**: PostgreSQL via EF Core. `OrderSphereDbContext` in `src/OrderSphere.Infrastructure/Persistence/`. Configurations applied via `ApplyConfigurationsFromAssembly`.
- **Email**: Azure Communication Services. `IEmailService` in `src/OrderSphere.Infrastructure/Email/`. Settings under `MailServiceConfiguration` in `appsettings.json`.
- **Service Bus**: Azure Service Bus. Publisher in `src/OrderSphere.Infrastructure/ServiceBus/`. Queue `orders` carries `CheckoutCartEvent` for asynchronous order processing.

## Ask before

- Adding a NuGet dependency.
- Schema changes that aren't trivially backward-compatible.
- Introducing a new architectural pattern or cross-cutting concern.
- Changing authentication or authorization flow.
- Breaking a public contract consumed by the UI client.

Proceed without asking for: bug fixes, refactors inside one layer, new features that follow an existing feature pattern, UI changes consistent with `copilot-instructions.md`, performance improvements with no behavior change.

## Commit format

Conventional commits: `feat:`, `fix:`, `refactor:`, `docs:`, `style:`, `test:`, `chore:`. One-line subject describing the user-visible change; body for rationale if non-obvious.
