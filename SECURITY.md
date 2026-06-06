# Security Policy

## Supported versions

OrderSphere is an actively developed showcase project. Security fixes are applied to the
`master` branch only; there are no maintained release branches.

| Version | Supported |
|---|---|
| `master` (latest) | Yes |
| Older commits / tags | No |

## Reporting a vulnerability

Please do not open public GitHub issues for security vulnerabilities.

Report privately through either channel:

- **GitHub Private Vulnerability Reporting** — open the repository's *Security* tab and choose
  *Report a vulnerability*. This is the preferred channel.
- **Email** — moritzwaldau99@gmail.com. Use a subject line beginning with `SECURITY:`.

Please include:

- A description of the issue and its impact.
- Steps to reproduce, or a proof of concept.
- Affected components (service, gateway, BFF, building block) and the relevant commit.

## What to expect

- Acknowledgement of your report within five business days.
- An initial assessment and severity classification thereafter.
- Coordinated disclosure once a fix is available. Reporters are credited on request.

## Scope

In scope: authentication and authorization (Keycloak/BFF/gateway), input validation, data
exposure across service boundaries, and the CI/CD supply chain (`.github/workflows`).

Out of scope: findings that require a compromised developer machine, issues in third-party
dependencies that already have a published advisory (these are tracked via Dependabot and the
dependency-review workflow), and the intentionally empty demo credentials seeded by
`contracts/keycloak/seed-dev-passwords.ps1` for local development.
