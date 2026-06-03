using '../main.bicep'

// --- Naming / region -------------------------------------------------------
param namePrefix = 'ssodev'
param location = 'northeurope'

param tags = {
  workload: 'ordersphere-sso'
  environment: 'dev'
  managedBy: 'bicep'
}

// --- Issuer / hostname -----------------------------------------------------
// Empty = use the Container Apps default FQDN (no domain needed). The issuer
// becomes https://keycloak.<env-default-domain>/realms/ordersphere and is shown
// in the deployment output `keycloakRealmAuthority`.
// To switch to a custom domain later: set the hostname here, run pass 1, create
// the asuid TXT + CNAME records, then set enableCustomDomain = true and redeploy.
param ssoHostname = ''

// --- Keycloak image --------------------------------------------------------
// Set by the pipeline to the freshly built tag, e.g. ssodevacr.azurecr.io/keycloak:<sha>.
param keycloakImage = 'ssodevacr.azurecr.io/keycloak:latest'

// --- Postgres admin --------------------------------------------------------
param postgresAdminUsername = 'kcadmin'

// --- Keycloak bootstrap admin ----------------------------------------------
param keycloakAdminUsername = 'admin'

// --- Custom domain ---------------------------------------------------------
// Pass 1: false. After the asuid TXT + CNAME records exist, set to true and redeploy.
param enableCustomDomain = false

// --- Secrets ---------------------------------------------------------------
// Supplied at deploy time, NOT stored here. The pipeline passes them via
// `--parameters postgresAdminPassword=$(...) keycloakAdminPassword=$(...)`
// sourced from GitHub Actions secrets. Local: use environment expansion.
param postgresAdminPassword = readEnvironmentVariable('POSTGRES_ADMIN_PASSWORD', '')
param keycloakAdminPassword = readEnvironmentVariable('KEYCLOAK_ADMIN_PASSWORD', '')
