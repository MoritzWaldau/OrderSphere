# Security Policy

## Supported versions

OrderSphere is a reference implementation. Only the latest `master` is maintained; there are no
backported security fixes for older tags.

| Version | Supported |
|---|---|
| `master` (latest) | Yes |
| Older tags | No |

## Reporting a vulnerability

Report suspected vulnerabilities privately. Do **not** open a public issue for security reports.

- **Preferred:** open a [GitHub private security advisory](https://github.com/MoritzWaldau/OrderSphere/security/advisories/new).
- **Alternative:** email moritzwaldau99@gmail.com with the subject `OrderSphere security`.

Please include affected component(s), reproduction steps, and impact assessment. You will receive
an acknowledgement within 5 business days. Once a fix is available, the advisory is published and
the reporter credited unless anonymity is requested.

## Automated controls

The following run in CI on every push and pull request to `master`:

- **CodeQL** — static analysis for code-level vulnerabilities.
- **Dependency review** — flags vulnerable or newly introduced dependencies on pull requests.
- **Dependabot** — automated dependency and GitHub Actions updates.
- **Vulnerable-package scan** — `dotnet list package --vulnerable --include-transitive`; fails the build when a vulnerable package is detected.
- **Gitleaks** — secret scanning on every push and pull request.
- **Trivy** — filesystem and misconfiguration scanning; SARIF results uploaded to the Security tab.

Secrets are held in Azure Key Vault outside development; development uses .NET user-secrets.
Authentication is delegated to Auth0 (OIDC); no credentials are stored in the application.
