# 0005 — HTTP clients + Service Bus events instead of gRPC

**Status:** Accepted

## Context

Services must communicate without referencing each other's projects. Two interaction styles are
needed: synchronous request/response (e.g. Basket validating stock against Catalog) and asynchronous,
decoupled propagation (e.g. order placed → notification, payment, webhooks). gRPC/Protobuf was an
option for the synchronous path and was scaffolded as a contracts folder, but never adopted.

## Decision

- **Synchronous:** typed HTTP client interfaces (`I<Producer>Client`) declared in the consumer's
  `Application/Abstractions/` and implemented in `Infrastructure/` against the producer's REST API.
  Requests/responses are consumer-owned `record` DTOs; methods return `Result<T>`.
- **Asynchronous:** integration events as JSON on Azure Service Bus queues, defined as shared records
  in `BuildingBlocks.Contracts` and published through a transactional outbox via `IEventBus`.
- **gRPC/Protobuf and published OpenAPI/NuGet contract packages are out of scope.** The empty
  `contracts/proto/` and `contracts/openapi/` folders are placeholders only.

See [../../contracts/CONVENTIONS.md](../../contracts/CONVENTIONS.md) for the full conventions.

## Consequences

- One transport (HTTP/JSON) and one messaging mechanism (Service Bus) to operate, debug, and observe
  — no Protobuf toolchain or codegen pipeline.
- Contracts are plain C# (HTTP DTOs and event records); changes to event records are public-contract
  changes requiring sign-off.
- No cross-language strong typing or streaming benefits that gRPC would provide; acceptable for an
  all-.NET system.
- Adopting gRPC later is a deliberate architectural change, not a drop-in.

## Alternatives considered

- **gRPC for synchronous calls** — rejected for now: extra toolchain and codegen for an all-.NET
  estate; HTTP clients suffice.
- **Published NuGet contract packages per service** — rejected: in-repo `BuildingBlocks.Contracts`
  plus consumer-owned HTTP DTOs avoid versioned-package overhead.
