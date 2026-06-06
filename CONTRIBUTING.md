# Contributing to OrderSphere

Thanks for your interest in OrderSphere. This guide covers the essentials for getting a change
merged. Architecture and behavioral conventions live in [CLAUDE.md](CLAUDE.md) and
[docs/architecture.md](docs/architecture.md) — read those before making non-trivial changes.

## Prerequisites

- .NET 10 SDK (the exact version is pinned in [global.json](global.json)).
- A container runtime (Docker Desktop or Podman) for the Aspire-managed dependencies.

See the [README](README.md#getting-started) for the local quickstart.

## Development workflow

1. Create a branch off `master` (`feat/...`, `fix/...`, `chore/...`).
2. Make your change, following the conventions in [CLAUDE.md](CLAUDE.md).
3. Build, format, and test locally before opening a pull request.
4. Open a PR against `master` and fill in the pull request template.

### Build, format, test

Run from the repository root:

```bash
# Build
dotnet build OrderSphere.slnx

# Run the full system via Aspire
dotnet run --project src/Hosting/OrderSphere.AppHost

# Format check (CI runs this as a blocking gate; generated migrations are excluded)
dotnet format OrderSphere.slnx --verify-no-changes --exclude "**/Migrations/**"

# All tests (CI enforces a 70% branch-coverage gate via .runsettings)
dotnet test
```

## Coding conventions

These are summarized from [CLAUDE.md](CLAUDE.md); that file is authoritative:

- Microservices over Clean Architecture with CQRS (MediatR) and DDD; dependencies point inward
  toward the domain.
- Business validation returns a `Result<T>` failure. Exceptions are reserved for genuinely
  exceptional conditions.
- Commands and queries return `Result<TDto>`. DTOs are `record` types; entities are `class` types
  inheriting `AuditableEntity`.
- Features live in `Features/<Aggregate>/<UseCase>/`. Handlers depend on the
  `I<Service>DbContext` interface, never the concrete context.
- Cross-service calls go through typed HTTP clients or Service Bus integration events — never a
  direct project reference across service boundaries.
- Queries against soft-deletable entities must filter `!x.IsDeleted`.
- All I/O is `async`/`await`. Nullable reference types are enabled; treat warnings as real.

## Commit and PR conventions

- Commits and **PR titles** follow [Conventional Commits](https://www.conventionalcommits.org/):
  `feat:`, `fix:`, `refactor:`, `docs:`, `style:`, `test:`, `chore:`. The PR title is linted in
  CI and becomes the squash-merge commit message on `master`.
- Keep the subject line to one user-visible change; use the body for rationale when non-obvious.

## Before you open a PR

- [ ] `dotnet build OrderSphere.slnx` succeeds.
- [ ] `dotnet format ... --verify-no-changes` is clean.
- [ ] `dotnet test` passes and coverage meets the gate.
- [ ] Public contracts consumed by the UI client are unchanged, or the change is described in the
      PR.

## Reporting security issues

Do not file public issues for vulnerabilities. Follow [SECURITY.md](SECURITY.md).
