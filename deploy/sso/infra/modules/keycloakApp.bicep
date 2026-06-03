// Keycloak 26 Container App (production mode) with Key Vault-backed secrets,
// ACR pull via UAMI, external HTTPS ingress, and health probes on port 9000.
//
// No custom domain: the issuer is the Container Apps default FQDN
// "keycloak.<env-default-domain>", deterministic before the app exists.
// To add a custom domain later, create a managed certificate against the
// environment and add the domain to ingress.customDomains (separate change).
@description('Azure region.')
param location string
@description('Resource tags.')
param tags object

param environmentId string
@description('Container Apps Environment default domain (e.g. "<hash>.northeurope.azurecontainerapps.io").')
param environmentDefaultDomain string
@description('Full Keycloak image reference (ACR).')
param image string
param userAssignedIdentityId string
param acrLoginServer string
param keyVaultUri string

@description('Optional custom issuer hostname. Empty = use the Container Apps default FQDN.')
param ssoHostname string = ''

param postgresFqdn string
param postgresDatabase string
param postgresUsername string
param keycloakAdminUsername string

param cpu string = '1.0'
param memory string = '2.0Gi'

// App name is fixed, so the default FQDN is "keycloak.<env-default-domain>".
var effectiveHostname = empty(ssoHostname) ? 'keycloak.${environmentDefaultDomain}' : ssoHostname

resource app 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'keycloak'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentityId}': {}
    }
  }
  properties: {
    managedEnvironmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      registries: [
        {
          server: acrLoginServer
          identity: userAssignedIdentityId
        }
      ]
      secrets: [
        {
          name: 'db-password'
          keyVaultUrl: '${keyVaultUri}secrets/db-password'
          identity: userAssignedIdentityId
        }
        {
          name: 'kc-admin-password'
          keyVaultUrl: '${keyVaultUri}secrets/keycloak-admin-password'
          identity: userAssignedIdentityId
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'keycloak'
          image: image
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          env: [
            { name: 'KC_DB', value: 'postgres' }
            { name: 'KC_DB_URL', value: 'jdbc:postgresql://${postgresFqdn}:5432/${postgresDatabase}?sslmode=require' }
            { name: 'KC_DB_USERNAME', value: postgresUsername }
            { name: 'KC_DB_PASSWORD', secretRef: 'db-password' }
            { name: 'KC_HOSTNAME', value: 'https://${effectiveHostname}' }
            { name: 'KC_HTTP_ENABLED', value: 'true' }
            { name: 'KC_PROXY_HEADERS', value: 'xforwarded' }
            { name: 'KC_HEALTH_ENABLED', value: 'true' }
            { name: 'KC_METRICS_ENABLED', value: 'true' }
            { name: 'KC_BOOTSTRAP_ADMIN_USERNAME', value: keycloakAdminUsername }
            { name: 'KC_BOOTSTRAP_ADMIN_PASSWORD', secretRef: 'kc-admin-password' }
          ]
          probes: [
            {
              type: 'Startup'
              httpGet: {
                path: '/health/started'
                port: 9000
              }
              initialDelaySeconds: 20
              periodSeconds: 10
              failureThreshold: 30
            }
            {
              type: 'Liveness'
              httpGet: {
                path: '/health/live'
                port: 9000
              }
              periodSeconds: 30
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health/ready'
                port: 9000
              }
              periodSeconds: 15
              failureThreshold: 3
            }
          ]
        }
      ]
      scale: {
        // Single replica: the imported realm + bootstrap admin run a DB migration
        // on first start, and the default config is not clustered.
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

output appName string = app.name
output fqdn string = app.properties.configuration.ingress.fqdn
output issuerHostname string = effectiveHostname
output issuerUrl string = 'https://${effectiveHostname}/realms/ordersphere'
output customDomainVerificationId string = app.properties.customDomainVerificationId
