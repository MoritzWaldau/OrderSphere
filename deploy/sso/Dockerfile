# syntax=docker/dockerfile:1
# ---------------------------------------------------------------------------
# OrderSphere central SSO — optimized Keycloak 26 image.
#
# Single source of truth for the realm is contracts/keycloak/ordersphere-realm.json
# (the same file the local Aspire run imports). The CI step copies it into this
# build context as realm/ordersphere-realm.json, and the realm stage below injects
# the Azure BFF origin (login redirect, post-logout, back-channel logout) so the
# canonical file stays localhost-clean.
#
# Build (context = deploy/sso):
#   cp contracts/keycloak/ordersphere-realm.json deploy/sso/realm/ordersphere-realm.json
#   docker build -f deploy/sso/Dockerfile --build-arg BFF_BASE_URL=https://app.dev.example.com deploy/sso
# ---------------------------------------------------------------------------

ARG KEYCLOAK_VERSION=26.1

# --- Stage 1: transform realm (inject Azure BFF origin) --------------------
FROM alpine:3.23 AS realm
RUN apk add --no-cache jq
ARG BFF_BASE_URL=https://localhost:5001
COPY realm/ordersphere-realm.json /in/realm.json
COPY realm-transform.jq /in/transform.jq
RUN jq --arg bff "$BFF_BASE_URL" -f /in/transform.jq /in/realm.json > /out-realm.json

# --- Stage 2: optimized server build (bakes db vendor, health, metrics) ----
FROM quay.io/keycloak/keycloak:${KEYCLOAK_VERSION} AS builder
ENV KC_DB=postgres
ENV KC_HEALTH_ENABLED=true
ENV KC_METRICS_ENABLED=true
RUN /opt/keycloak/bin/kc.sh build

# --- Stage 3: runtime image -------------------------------------------------
FROM quay.io/keycloak/keycloak:${KEYCLOAK_VERSION}
COPY --from=builder /opt/keycloak/ /opt/keycloak/
COPY --from=realm /out-realm.json /opt/keycloak/data/import/ordersphere-realm.json
ENTRYPOINT ["/opt/keycloak/bin/kc.sh"]
# --import-realm only imports on first start; later admin changes persist in Postgres.
CMD ["start", "--optimized", "--import-realm"]
