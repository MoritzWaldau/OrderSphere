# Contributing to OrderSphere

OrderSphere is a reference implementation; contributions are welcome where they sharpen the
demonstrated patterns or fix defects. This document covers the workflow. Architectural rules and
conventions live in [CLAUDE.md](CLAUDE.md) and [docs/architecture.md](docs/architecture.md) — read
those before changing code.

## Prerequisites

- .NET 10 SDK (see [global.json](global.json) for the pinned version).
- A container runtime (Docker or Podman) for PostgreSQL, Redis, and the Service Bus emulator.

## Workflow

1. Branch from `master`. Use a descriptive prefix: `feat/`, `fix/`, `refactor/`, `docs/`, `chore/`.
2. Keep a change within a single layer or service where possible. No service references another
   service's projects directly — cross-service communication is HTTP clients or Service Bus events.
3. Add or update tests for behavioural changes.
4. Run the local checks below before opening a pull request.
5. Open a pull request against `master`. The PR title must follow Conventional Commits.

## Local checks

Run from the repository root:

| Task | Command |
|---|---|
| Build | `dotnet build OrderSphere.slnx` |
| Format (verify) | `dotnet format OrderSphere.slnx --verify-no-changes --severity warn --exclude "**/Migrations/**"` |
| All tests | `dotnet test` |
| One test project | `dotnet test tests/OrderSphere.Domain.Tests` |
| Single test by name | `dotnet test --filter "FullyQualifiedName~CheckoutCart"` |

CI additionally enforces a 70% branch-coverage gate, CodeQL, dependency review, and a
vulnerable-package scan. A pull request must pass these before merge.

## Commit messages

Conventional Commits: `feat:`, `fix:`, `refactor:`, `docs:`, `style:`, `test:`, `chore:`. Use a
one-line subject describing the user-visible change; add a body for non-obvious rationale.

## Ask before

- Adding a NuGet dependency.
- Schema changes that are not trivially backward-compatible.
- Introducing a new architectural pattern or cross-cutting concern.
- Changing authentication or authorization flow.
- Breaking a public contract consumed by the UI client.

See [CLAUDE.md](CLAUDE.md) for the full list of changes that proceed without prior discussion.
