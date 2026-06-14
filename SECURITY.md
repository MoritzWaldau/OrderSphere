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

## Threat model / trust boundaries

OrderSphere enforces security at well-defined boundaries rather than trusting the network. The trust
chain and the control at each hop:

| Boundary | Control |
|---|---|
| Browser → BFF | No tokens in the browser. The BFF owns the OIDC session in an encrypted HTTP-only cookie; CSRF protection is applied at the BFF. See [ADR 0003](docs/adr/0003-bff-and-api-gateway.md). |
| BFF → API gateway | Server-side proxied calls; the BFF attaches the access token. The gateway (`OrderSphere.ApiGateway`) is the single external API ingress. |
| Gateway → services | Each service validates the JWT (issuer/audience) independently and enforces per-service RBAC policies. See [auth/role-model.md](docs/auth/role-model.md). |
| Advisory → MCP → services | The end-user's bearer token is forwarded down the chain (`BFF → Advisory.Api → MCP.Server → Gateway → services`), so AI tools stay scoped to the caller. User-scoped tools return no data without a valid token. See [architecture.md](docs/architecture.md#identity-forwarding). |
| Workers → services (M2M) | Background workers authenticate with Auth0 `client_credentials` machine identities (`svc.*` roles), not user tokens. |
| Application → secrets | No credentials in code. Secrets come from Azure Key Vault (non-dev) or .NET user-secrets (dev); Redis uses Entra ID (no password in the connection string). |

Data is correlated across services by a deterministic identity derivation, never by a shared user
store or cross-service foreign keys ([ADR 0002](docs/adr/0002-customerid-deterministic-guid-from-sub.md)).

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
