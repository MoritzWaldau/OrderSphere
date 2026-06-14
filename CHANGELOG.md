# Changelog

All notable changes to OrderSphere are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project
adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html). Entries are maintained
manually.

## [Unreleased]

### Added
- Repository documentation: `LICENSE`, `SECURITY.md`, `CONTRIBUTING.md`, and this changelog.
- README status, deployment, security, and contributing sections with CI/security badges.
- README: local-secrets setup, configuration reference, repository layout, testing/quality,
  screenshots, and roadmap sections.
- `docs/README.md` documentation index.
- Architecture Decision Records under `docs/adr/` (0001–0006).
- `docs/glossary.md` (ubiquitous language) and `docs/operations.md` (observability runbook).
- `SECURITY.md`: threat-model / trust-boundaries section.

### Changed
- Corrected AppHost project path to `src/Hosting/OrderSphere.AppHost` across README, `CLAUDE.md`,
  and `docs/architecture.md`; corrected `OrderSphere.Web` path to `src/Frontend/OrderSphere.Web`.
- `contracts/CONVENTIONS.md` rewritten to describe the actual HTTP-client + `BuildingBlocks.Contracts`
  architecture; gRPC/OpenAPI/NuGet contract packages marked out of scope.
- `docs/architecture.md`: corrected the Service Bus queue inventory to match `AppHost.cs`.
- `docs/deploy-ordersphere.md`: fixed step numbering.
