# OrderSphere — Operations Reference (DEV)

Operational runbooks for the Azure Container Apps DEV environment. Infrastructure is
provisioned via `azd`; Container Apps names match the resource names in `src/infra/*.tmpl.yaml`.

## Container App inventory

| Container App | Type | Public ingress |
|---|---|---|
| `ordersphere-bff` | Web BFF (Blazor WASM host) | Yes |
| `ordersphere-apigateway` | YARP API Gateway | Internal only |
| `ordersphere-catalog` | Catalog API | Internal only |
| `ordersphere-basket` | Basket API | Internal only |
| `ordersphere-ordering` | Ordering API | Internal only |
| `ordersphere-ordering-worker` | Ordering background worker | Internal only |
| `ordersphere-payment` | Payment API | Internal only |
| `ordersphere-payment-worker` | Payment background worker | Internal only |
| `ordersphere-userprofile` | UserProfile API | Internal only |
| `ordersphere-webhooks` | Webhooks API | Internal only |
| `ordersphere-webhooks-worker` | Webhooks background worker | Internal only |
| `ordersphere-notification-worker` | Notification background worker | Internal only |

## Post-deploy smoke test

Run after every `azd deploy`. Commands assume the Azure CLI is authenticated and
`RESOURCE_GROUP` is set to the environment resource group (default: `rg-<AZURE_ENV_NAME>`).

```bash
RESOURCE_GROUP="rg-${AZURE_ENV_NAME}"
```

### 1 — BFF public health check

```bash
BFF_FQDN=$(az containerapp show \
  --name ordersphere-bff \
  --resource-group "$RESOURCE_GROUP" \
  --query "properties.configuration.ingress.fqdn" \
  --output tsv)

curl --fail --silent --max-time 30 "https://${BFF_FQDN}/health/live" && echo "OK"
```

Expected: HTTP 200. If not, check Container Apps logs:

```bash
az containerapp logs show --name ordersphere-bff --resource-group "$RESOURCE_GROUP" --tail 50
```

### 2 — All Container Apps running

```bash
for APP in ordersphere-bff ordersphere-apigateway ordersphere-catalog ordersphere-basket \
           ordersphere-ordering ordersphere-ordering-worker ordersphere-payment \
           ordersphere-payment-worker ordersphere-userprofile ordersphere-webhooks \
           ordersphere-webhooks-worker ordersphere-notification-worker; do
  STATE=$(az containerapp revision list \
    --name "$APP" \
    --resource-group "$RESOURCE_GROUP" \
    --query "[0].properties.runningState" \
    --output tsv 2>/dev/null || echo "unknown")
  echo "$APP: $STATE"
done
```

Expected: every app reports `Running`.

### 3 — End-to-end login flow (manual)

1. Open `https://${BFF_FQDN}` in a browser.
2. Authenticate against the external Keycloak DEV realm (`ordersphere`).
3. Navigate to the product catalog — verify products are listed.
4. Add a product to the basket.
5. Check out — verify order confirmation message and tracking number appear.

If step 2 fails, verify:
- `keycloak-realm-authority` parameter in Key Vault matches the DEV realm URL.
- BFF redirect URI in Keycloak includes `https://${BFF_FQDN}/signin-oidc`.

## Rollback procedure

Azure Container Apps revision model: every `azd deploy` creates a new revision. Traffic is
directed to the active revision. Rolling back means activating the previous revision.

### Step 1 — Identify the previous revision

```bash
APP=ordersphere-bff   # repeat for each affected app
az containerapp revision list \
  --name "$APP" \
  --resource-group "$RESOURCE_GROUP" \
  --query "[].{name:name, active:properties.active, created:properties.createdTime}" \
  --output table
```

### Step 2 — Activate the previous revision

```bash
PREVIOUS_REVISION="<revision-name-from-step-1>"

az containerapp revision activate \
  --name "$APP" \
  --resource-group "$RESOURCE_GROUP" \
  --revision "$PREVIOUS_REVISION"

az containerapp ingress traffic set \
  --name "$APP" \
  --resource-group "$RESOURCE_GROUP" \
  --revision-weight "$PREVIOUS_REVISION=100"
```

Repeat for each Container App that received the bad deploy. The current revision remains
registered but receives 0 % traffic; it can be deactivated once root cause is confirmed.

### Step 3 — Deactivate the bad revision (optional, after confirmation)

```bash
BAD_REVISION="<bad-revision-name>"
az containerapp revision deactivate \
  --name "$APP" \
  --resource-group "$RESOURCE_GROUP" \
  --revision "$BAD_REVISION"
```

## Database migration rollback

EF Core does not support automatic down-migrations in production. If a schema change must
be reverted:

1. Identify the target migration name with `dotnet ef migrations list`.
2. Generate a rollback script:
   ```bash
   dotnet ef migrations script <TargetMigration> <CurrentMigration> \
     --project src/Services/<Service>/<Service>.Infrastructure \
     --startup-project src/Services/<Service>/<Service>.Api
   ```
3. Review the generated SQL and apply it manually against the DEV Postgres Flexible Server.
4. Deploy the application version that matches the target migration.

## Secret rotation

Secrets are stored in Azure Key Vault and bound to Container Apps via managed identity. To
rotate a secret:

1. Update the secret value in Key Vault.
2. Trigger a new revision (or restart the container app to pick up the updated secret
   reference if using Key Vault references instead of `azd`-injected env vars).

```bash
az containerapp update --name "$APP" --resource-group "$RESOURCE_GROUP"
```

This forces a new revision that re-reads the Key Vault reference.
