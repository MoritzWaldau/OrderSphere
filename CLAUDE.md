# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# Claude Code Instructions for OrderSphere

> Architecture reference (project layout, per-service tables, EF migration commands,
> external service wiring) lives in [docs/architecture.md](docs/architecture.md).
> Read it when you need a map of the system; it is not repeated here.

## Operating rules

- Audience for any document or explanation: enterprise architects. No marketing language, no slogans, no enthusiasm filler, no "not just X — it's Y" comparisons.
- Do not guess technical facts. If a concept, API, or library behavior isn't certain, research it before stating it.
- Documents are markdown. Diagrams are mermaid.
- State results and decisions directly.

## Architecture

Microservices over Clean Architecture with CQRS (MediatR) and DDD. Each service is independently deployable and owns its domain, persistence, and infrastructure. Errors flow through `Result<T>`, not exceptions. Entities carry audit fields and soft-delete via `AuditableEntity`.

Layer dependency direction per service: `Api → Application → Infrastructure → Domain → BuildingBlocks.Domain`. No service references another service's projects directly — cross-service communication uses HTTP clients or Service Bus events. Shared primitives live in `BuildingBlocks` (see [docs/architecture.md](docs/architecture.md)).

## Conventions

These are the rules that are not derivable by reading existing code. For everything else, the existing handlers and configurations are the template — read one before writing a new one.

- Business validation returns a `Result<T>` failure. Exceptions are reserved for genuinely exceptional conditions (I/O failures, programmer errors).
- Commands and queries return `Result<TDto>`. DTOs are `record` types; entities are `class` types.
- New entities inherit `AuditableEntity`. Add a matching EF configuration in the service's `Infrastructure/EntityConfigurations/`.
- Features live in `Features/<Aggregate>/<UseCase>/`, co-locating command/query, handler, and validator. Catalog uses `Catalog.Application/Features/`; all other services use `<Service>.Api/Features/`.
- Cross-service calls go through typed HTTP client interfaces (e.g. `ICatalogClient`, `IBasketClient`). No direct project references across service boundaries.
- Integration events are defined in `BuildingBlocks.Contracts`. Publish via `IEventBus`; consume via `IIntegrationEventHandler<T>`.
- Queries against soft-deletable entities must filter `!x.IsDeleted`.
- All I/O is `async`/`await`. No `.Result`, no `.Wait()`, no `.GetAwaiter().GetResult()`.
- Nullable reference types are enabled. Treat warnings as real.

## UI and styling

All visual, theming, MudBlazor, and CSS rules live in `.github/copilot-instructions.md`. Do not duplicate them here. When making UI changes, read that file first.

## Commands

Run from the repository root. Full EF migration matrix is in [docs/architecture.md](docs/architecture.md).

| Task | Command |
|---|---|
| Build | `dotnet build OrderSphere.slnx` |
| Run via Aspire | `dotnet run --project src/OrderSphere.AppHost` |
| Run BFF (with WASM) | `dotnet run --project src/Gateways/OrderSphere.Bff` |
| All tests | `dotnet test` |
| One test project | `dotnet test tests/OrderSphere.Domain.Tests` |
| Single test / by name | `dotnet test --filter "FullyQualifiedName~CheckoutCart"` (also accepts `Name~`, `ClassName~`) |

## Ask before

- Adding a NuGet dependency.
- Schema changes that aren't trivially backward-compatible.
- Introducing a new architectural pattern or cross-cutting concern.
- Changing authentication or authorization flow.
- Breaking a public contract consumed by the UI client.

Proceed without asking for: bug fixes, refactors inside one layer, new features that follow an existing feature pattern, UI changes consistent with `copilot-instructions.md`, performance improvements with no behavior change.

## Commit format

Conventional commits: `feat:`, `fix:`, `refactor:`, `docs:`, `style:`, `test:`, `chore:`. One-line subject describing the user-visible change; body for rationale if non-obvious.
