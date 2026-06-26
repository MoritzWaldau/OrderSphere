---
paths:
  - "src/**/Application/Features/**"
  - "src/**/Application/Abstractions/**"
---

CQRS/feature conventions:
- Commands and queries return `Result<TDto>`. Business validation = `Result<T>` failure; exceptions = I/O failures and programmer errors only.
- DTOs are `record` types; entities are `class` types.
- Features co-locate command/query, handler, and validator under `Features/<Aggregate>/<UseCase>/`.
- Handlers depend on `I<Service>DbContext` (declared in `<Service>.Application/Abstractions/`), never on the concrete context.
- Cross-service calls go through typed HTTP client interfaces — no direct project references across service boundaries.
- Integration events are defined in `BuildingBlocks.Contracts`; publish via `IEventBus`.

Read an existing handler in the same service before writing a new one — it is the canonical template.
