# Keycloak Production Deployment

## Topology

```
                    ┌──────────────────────────────┐
                    │   Load Balancer (sticky)      │
                    └────────────┬─────────────────┘
                                 │
              ┌──────────────────┴───────────────────┐
              │                                       │
  ┌───────────▼──────────┐             ┌──────────────▼──────────┐
  │  Keycloak replica 1  │             │  Keycloak replica 2      │
  │  KC_CACHE=ispn       │◄── Infinispan cluster ──►│  KC_CACHE=ispn          │
  └───────────┬──────────┘             └──────────────┬──────────┘
              │                                        │
              └──────────────┬─────────────────────────┘
                             │
              ┌──────────────▼──────────────┐
              │  Postgres (dedicated)        │
              │  Managed Identity connection │
              └─────────────────────────────┘
```

Two replicas behind a sticky-session load balancer (session affinity is required for the Keycloak admin console; application-layer sessions are stateless). `KC_CACHE=ispn` enables the built-in Infinispan distributed cache for session/user-session replication between replicas.

## Environment Variables

| Variable | Value | Notes |
|---|---|---|
| `KC_DB` | `postgres` | |
| `KC_DB_URL` | `jdbc:postgresql://{host}/{db}` | Managed identity or connection string from Key Vault |
| `KC_HOSTNAME` | `https://auth.ordersphere.example.com` | Public-facing Keycloak hostname |
| `KC_HOSTNAME_ADMIN` | `https://auth-admin.ordersphere.internal` | Admin console — restricted to internal network |
| `KC_HTTP_ENABLED` | `false` | TLS only |
| `KC_HTTPS_CERTIFICATE_FILE` | `/opt/keycloak/conf/tls.crt` | Mounted from secret |
| `KC_HTTPS_CERTIFICATE_KEY_FILE` | `/opt/keycloak/conf/tls.key` | Mounted from secret |
| `KC_CACHE` | `ispn` | Distributed cache for HA |
| `KC_PROXY_HEADERS` | `xforwarded` | Required when behind a load balancer |
| `KEYCLOAK_ADMIN` | `admin` | Admin username |
| `KEYCLOAK_ADMIN_PASSWORD` | (from Key Vault) | Rotated per `docs/auth/secrets-rotation.md` |

## Start Command

Production containers use `start` (not `start-dev`):

```sh
/opt/keycloak/bin/kc.sh start \
  --import-realm \
  --optimized
```

Build the optimized image with the realm pre-imported:

```dockerfile
FROM quay.io/keycloak/keycloak:26.1 AS builder
COPY ordersphere-realm.json /opt/keycloak/data/import/
RUN /opt/keycloak/bin/kc.sh build

FROM quay.io/keycloak/keycloak:26.1
COPY --from=builder /opt/keycloak/ /opt/keycloak/
ENTRYPOINT ["/opt/keycloak/bin/kc.sh"]
```

## Realm Import

The authoritative realm definition is `contracts/keycloak/ordersphere-realm.json`. It is imported on first start. Subsequent changes must be applied via:

1. Keycloak admin REST API (preferred for production: targeted, audited updates).
2. Full realm export → diff → re-import on a fresh instance (for destructive changes only).

## Back-Channel Logout URL

The `web-bff` client's `backchannel.logout.url` attribute (currently empty in the realm JSON) **must be set** before the first production deployment:

```
https://{bff-host}/bff/backchannel-logout
```

Set via the Keycloak admin console under **Clients → web-bff → Advanced → Back Channel Logout URL**, or via the admin REST API:

```sh
curl -X PUT \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"attributes":{"backchannel.logout.url":"https://bff.ordersphere.example.com/bff/backchannel-logout"}}' \
  https://auth.ordersphere.example.com/admin/realms/ordersphere/clients/{client-uuid}
```

## JWT Signing Key Rotation

Keycloak manages signing key rotation automatically via the key provider priority list. The `rsa-generated` provider generates a new key pair periodically and publishes all active public keys in the JWKS endpoint (`/realms/ordersphere/protocol/openid-connect/certs`). Services using `AddOrderSphereJwtAuth()` refresh JWKS every 10 minutes (`MetadataRefreshInterval`) and automatically every 24 hours, so old public keys remain verifiable for at least one full refresh cycle.

Manual rotation: navigate to **Realm Settings → Keys → rsa-generated** and click **Rotate**. Old keys remain in JWKS for the `rotatedKeyExpiry` period (default: 30 days).

## Database Sizing

The Keycloak database is separate from the application databases (`ordersphere-db`, `catalog-db`, `ordering-db`, `userprofile-db`). Recommended minimum: 2 vCPU, 4 GB RAM, 20 GB SSD for up to 100 000 users.

## Health Checks

- Liveness: `GET /health/live`
- Readiness: `GET /health/ready`

Both are exposed by Keycloak's built-in health endpoint (`--health-enabled=true`).
