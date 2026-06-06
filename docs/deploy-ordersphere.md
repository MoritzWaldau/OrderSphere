# OrderSphere — Azure deployment (azd)

Deploying OrderSphere into a dedicated Azure DEV environment via the Azure Developer CLI
(`azd`). OrderSphere is a .NET Aspire application: the AppHost manifest
(`src/OrderSphere.AppHost`) is the single source of the resource topology. `azd` reads the
manifest and generates the Bicep templates from it (Container Apps, PostgreSQL Flexible Server,
Service Bus, Azure Managed Redis, Key Vault). There is deliberately **no** hand-maintained
`infra/` folder.

Keycloak is **not** part of this deployment. It runs as an independent central SSO provider
(see [`deploy/sso/`](../deploy/sso/README.md)). The coupling is solely through the issuer URL and
four client secrets.

## Prerequisites

- `azd` installed (`azd version`), `dotnet` 10 SDK.
- An Azure subscription with permission to create the resource group `rg-ordersphere-dev`.
- A running cloud Keycloak (issuer URL known).
- Docker (for the local container build performed by `azd`).

## Key facts

| Key | Value |
|---|---|
| Environment | `dev` |
| Region | `northeurope` |
| Resource Group | `rg-dev` |
| BFF FQDN | `ordersphere-bff.kindtree-ed135723.northeurope.azurecontainerapps.io` |
| Issuer | `https://keycloak.salmoncoast-4abe9a09.northeurope.azurecontainerapps.io/realms/ordersphere` |

> **Resource Group:** The Aspire azd path derives the resource group from the environment name
> (`rg-<env>` → `rg-dev`) and ignores an `AZURE_RESOURCE_GROUP` set afterwards. The infrastructure
> therefore lives in `rg-dev`. For `azd deploy` to write the Container Apps into the same group,
> `AZURE_RESOURCE_GROUP=rg-dev` must be set (otherwise 404 `ResourceGroupNotFound`).

## Database schema

Each service calls `Database.Migrate()` at startup (ungated, not restricted to Development —
see e.g. `src/Services/Catalog/.../Program.cs`). In the cloud, each container therefore migrates its
own schema against the PostgreSQL Flexible Server on first start. No separate migration step is
required.

## Redis authentication (Entra ID)

`AddAzureManagedRedis` provisions Azure Managed Redis with Microsoft Entra ID authentication
(access keys disabled); the managed identity is assigned a data-access policy. The connection string
injected by Aspire contains **no** password. `Aspire.StackExchange.Redis` ships no Entra token logic —
a raw `ConnectionMultiplexer.Connect(connectionString)` therefore fails with
`NOAUTH - connection has not yet authenticated`.

The services consequently build the Redis connection through `AddOrderSphereRedisAsync`
(`OrderSphere.ServiceDefaults/RedisExtensions.cs`): for an Azure Redis endpoint without a password,
`Microsoft.Azure.StackExchangeRedis` obtains an Entra token via the user-assigned managed identity
(`AZURE_CLIENT_ID`) and renews it automatically. The token-authenticated `IConnectionMultiplexer` is
shared by DistributedCache (Catalog, Ordering, BFF), the DataProtection key ring, and the SignalR
backplane (BFF). Locally (Aspire dev container) the same code connects without credentials.

## Step by step

### 1. Sign in and create the environment
```powershell
azd auth login
azd env new dev --location northeurope --subscription <SUBSCRIPTION_ID>
azd env set AZURE_RESOURCE_GROUP rg-dev
```

> **Important:** Aspire parameters with a hyphen (`keycloak-realm-authority`,
> `payment-bypass-providers`, the four `*-secret`) must **not** be set via `azd env set` —
> `azd env set` writes to the `.env`, where hyphens in variable names are invalid
> (`unexpected character "-" in variable name`). `azd up` prompts for these parameters
> interactively on the first deploy and stores them correctly (non-secrets in `config.json`, secrets
> as a Key Vault reference). Only `AZURE_*` values (underscores) belong in the `.env`.

### 2. Generate real client secrets
Generate four new random secrets (one per confidential client) and note them down:
```powershell
foreach ($c in 'web-bff','ordering-worker','notification-worker','payment-worker') {
  $s = [Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Max 256 }))
  Write-Host "$c = $s"
}
```
Set each value in the **cloud Keycloak admin console**
(*Clients → \<client\> → Credentials → Regenerate/Set*). The running instance has already imported
the realm into Postgres; changes to `contracts/keycloak/ordersphere-realm.json` are **not** picked up
automatically.

> Note: `contracts/keycloak/ordersphere-realm.json` keeps the `*-dev-secret-change-in-prod`
> placeholders. Real secrets do not belong in Git — only in Keycloak and the Key Vault.

### 3. Deploy
```powershell
azd up
```
`azd up` prompts for the parameter values one after another:
- `keycloak-realm-authority` → the issuer URL (see Key facts above)
- `payment-bypass-providers` → `true`
- `bff-client-secret`, `ordering-worker-secret`, `notification-worker-secret`,
  `payment-worker-secret` → the values generated in step 2 (stored as Key Vault secrets)

It then provisions the infrastructure in `rg-ordersphere-dev` and deploys all 12 projects as
Container Apps. Only `ordersphere-bff` receives an external ingress (`WithExternalHttpEndpoints()` in
the AppHost). The answers are stored; later `azd up` runs do not prompt again.

### 6. Reconcile Keycloak against the BFF URL
After the deploy, determine the public BFF FQDN:
```powershell
azd show
```
On the `web-bff` client in the Keycloak admin console, add:
- Redirect URI: `https://<bff-fqdn>/*`
- Web Origin: `https://<bff-fqdn>`
- Post-logout redirect URI: `https://<bff-fqdn>/*`
- Backchannel logout URL: `https://<bff-fqdn>/bff/backchannel-logout`

## Verification

1. `azd up` runs without errors; resources present in `rg-ordersphere-dev`.
2. Service logs show a successful JWKS fetch from the Keycloak FQDN.
3. `https://<bff-fqdn>` → login redirect to Keycloak → successful return after step 6.
4. An authenticated API call (per-service audience validation) returns 200.
5. A `client_credentials` token for `ordering-worker`/`payment-worker` with the real secret →
   a valid token with role `svc.*`.

## CI/CD — `azd pipeline config`

Run only **after** the first successful `azd up`. The command sets up OIDC
(Workload Identity Federation), creates/uses the service principal, sets the GitHub
repo variables (`AZURE_ENV_NAME`, `AZURE_LOCATION`, `AZURE_SUBSCRIPTION_ID`) and propagates the
environment values including secret references. It generates the workflow `.github/workflows/azure-dev.yml`.

```powershell
azd pipeline config
```

Notes for this repository:

- **AppHost is not at the repo root.** The generated `azure-dev.yml` assumes the AppHost is in the
  root directory. Here it lives under `src/OrderSphere.AppHost`. In the **Provision Infrastructure**
  and **Deploy Application** steps, `working-directory: ./src/OrderSphere.AppHost` must therefore be
  added (see [Aspire docs on multi-project workflows](https://learn.microsoft.com/en-us/dotnet/aspire/deployment/azd/aca-deployment-github-actions)).
- **master is PR-protected.** `azd pipeline config` wants to commit/push the workflow — work on a
  branch and merge via PR rather than pushing directly to master.
- **Existing SSO credentials.** The SSO deployment already uses an OIDC federated credential and the
  `AZURE_*` repo secrets. `azd pipeline config` may create its own identity; if the existing one is to
  be reused, align the generated variables accordingly.
- The workflow runs `azd provision`/`azd deploy` and requires the .NET 10 SDK (container build by azd).

## Troubleshooting (actually encountered on the first deploy)

| Symptom | Cause | Resolution |
|---|---|---|
| `ConflictError: A vault with the same name already exists in deleted state` | An earlier, aborted `azd up` created the Key Vault and deleted it during cleanup; Key Vault soft-delete keeps the name reserved. | `az keyvault purge --name <vault> --location northeurope`, then `azd up` again. |
| `empty dotnet configuration output` (suggestion: `EnableSdkContainerSupport`) | Masks the real error of `dotnet publish` during the container build. Two observed triggers: (a) the Docker credential helper returns no ACR credentials; (b) a publish fails during a parallel build. | (a) see next row; (b) build the solution upfront with `dotnet build -c Release` (outputs warm) or deploy services individually with `azd deploy <service>`. |
| `docker-credential-desktop.EXE get … credentials not found in native keychain` | `~/.docker/config.json` has `credsStore: desktop`, but the Desktop keychain does not return the ACR token. | Remove the `credsStore` line from `~/.docker/config.json`, then `az acr login --name <acr>` (writes the token base64 directly into `config.json`). |
| Deploy step: `404 ResourceGroupNotFound: 'rg-ordersphere-dev'` | Infrastructure is in `rg-dev` (azd default `rg-<env>`); `AZURE_RESOURCE_GROUP` pointed to a non-existent group. | `azd env set AZURE_RESOURCE_GROUP rg-dev`, then `azd up`. |
| `/bff/login` → `500`, logs: `NOAUTH … connection has not yet authenticated` (Redis) | Azure Managed Redis enforces Entra ID auth; a raw Redis connection sends no token. | See section [Redis authentication](#redis-authentication-entra-id) — connection via `AddOrderSphereRedisAsync`. |
