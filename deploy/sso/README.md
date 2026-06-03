# Central SSO (Keycloak) — Azure DEV

Deployment assets for the central Keycloak 26 identity provider. This lives inside the OrderSphere repo but deploys **independently**: its own resource group (`rg-sso-dev`), its own lifecycle, and its own workflow (`.github/workflows/deploy-sso.yml`). The OrderSphere application deploy does not touch it.

The AppHost already excludes Keycloak in publish mode (`src/OrderSphere.AppHost/AppHost.cs`) and reads the issuer from the `keycloak-realm-authority` parameter — so services couple to this provider only through that URL and the realm contract.

## What gets deployed (all in `rg-sso-dev`)

Keycloak in **production mode** as an Azure Container App, behind external HTTPS ingress, backed by a dedicated PostgreSQL Flexible Server (private access). Secrets come from Key Vault via a user-assigned managed identity; the image is pulled from a dedicated ACR.

**No domain required.** By default the issuer is the Container Apps default FQDN:
`https://keycloak.<env-default-domain>/realms/ordersphere`. The deployment prints this as the output `keycloakRealmAuthority`. A custom domain is fully optional and can be added later (see below) — switching only changes the issuer URL, i.e. OrderSphere's `keycloak-realm-authority` parameter.

```
deploy/sso/
  Dockerfile                  # optimized KC 26 image; jq-injects Azure BFF origin into the realm
  realm/                      # realm is copied here from contracts/ at build time (gitignored)
  infra/
    main.bicep                # RG-scoped orchestration
    modules/*.bicep           # network, logAnalytics, identity, acr, keyvault, postgres, containerEnv, keycloakApp
    params/dev.bicepparam
```

Realm source of truth: `contracts/keycloak/ordersphere-realm.json` (shared with the local Aspire run). The Dockerfile injects the Azure BFF redirect/logout URLs and `sslRequired=external` at build time, leaving the canonical file localhost-clean.

## Prerequisites

1. Permission to create `rg-sso-dev` in the target subscription.
2. **OIDC federation** for GitHub Actions: an app registration / managed identity with a federated credential for this repo's `dev` environment, holding `Contributor` **and** `User Access Administrator` on the subscription or `rg-sso-dev` (the deploy creates AcrPull + Key Vault Secrets User role assignments).
3. Repo secrets: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `POSTGRES_ADMIN_PASSWORD`, `KEYCLOAK_ADMIN_PASSWORD`.

No domain or DNS zone is required for the default setup.

## Deploy (no domain)

Run the **Deploy SSO (Keycloak)** workflow. With the defaults (`SSO_HOSTNAME` empty, `enableCustomDomain = false`) it provisions everything and prints `keycloakRealmAuthority` — the issuer on the Container Apps default FQDN. Use that value as OrderSphere's `keycloak-realm-authority`.

`BFF_BASE_URL` can stay empty until the OrderSphere BFF is deployed; the realm then keeps localhost redirect URIs. Once the BFF's Azure URL is known, either set `BFF_BASE_URL` and re-run (rebuilds the image), or add the redirect URI + back-channel-logout URL for the `web-bff` client directly in the Keycloak admin console.

## Adding a custom domain later (optional, two-pass)

1. Set `ssoHostname` in `infra/params/dev.bicepparam` (and `SSO_HOSTNAME` in the workflow), keep `enableCustomDomain = false`, run **pass 1**. Read outputs `customDomainVerificationId` and `keycloakFqdn`.
2. In your DNS zone for `<ssoHostname>` (e.g. `sso.dev.example.com`):

   | Type | Name | Value |
   |---|---|---|
   | TXT | `asuid.sso.dev` | `<customDomainVerificationId>` |
   | CNAME | `sso.dev` | `<keycloakFqdn>` |

3. **Pass 2** — re-run with `enableCustomDomain = true`; the managed certificate validates via CNAME and binds. Update OrderSphere's `keycloak-realm-authority` to the new issuer.

## Local validation

```bash
az bicep build --file deploy/sso/infra/main.bicep
cp contracts/keycloak/ordersphere-realm.json deploy/sso/realm/ordersphere-realm.json
docker build -f deploy/sso/Dockerfile --build-arg BFF_BASE_URL=https://app.dev.example.com deploy/sso
```

## Verify

```bash
# <issuer> = the keycloakRealmAuthority output (default FQDN or your custom domain)
curl -s <issuer>/.well-known/openid-configuration | jq .issuer
# expect: <issuer>
```

Restart the revision and confirm realm + admin survive (state is in Postgres).

## Couple OrderSphere

In the OrderSphere DEV environment (separate deploy): set `keycloak-realm-authority` to the `keycloakRealmAuthority` deployment output, and align the client secrets between the realm and OrderSphere's Key Vault (`web-bff` ↔ `bff-client-secret`, `ordering-worker` ↔ `ordering-worker-secret`, `notification-worker` ↔ `notification-worker-secret`). The committed `*-dev-secret-change-in-prod` values are placeholders.

> `payment-worker` is referenced by the AppHost but absent from the realm — add it before wiring the payment worker's M2M calls.
