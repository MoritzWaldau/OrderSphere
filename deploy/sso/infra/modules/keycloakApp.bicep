// Keycloak 26 Container App (production mode) with Key Vault-backed secrets,
// ACR pull via UAMI, external HTTPS ingress, and health probes on port 9000.
//
// Custom domain is a TWO-PASS operation, gated by enableCustomDomain:
//   Pass 1 (false): deploy, read customDomainVerificationId + fqdn outputs.
//   -> create DNS: "asuid.<sub>" TXT = verification id, "<sub>" CNAME = fqdn.
//   Pass 2 (true):  the managed certificate validates via CNAME and binds.
@description('Azure region.')
param location string
@description('Resource tags.')
param tags object

param environmentId string
param environmentName string
@description('Container Apps Environment default domain (e.g. "<hash>.northeurope.azurecontainerapps.io"). Used to derive the issuer when no custom domain is set.')
param environmentDefaultDomain string
@description('Full Keycloak image reference (ACR).')
param image string
param userAssignedIdentityId string
param acrLoginServer string
param keyVaultUri string

@description('Custom issuer hostname, e.g. "sso.dev.example.com". Leave empty to use the Container Apps default FQDN.')
param ssoHostname string = ''
@description('Bind the custom domain + managed certificate (pass 2). Ignored when ssoHostname is empty.')
param enableCustomDomain bool = false

// Without a custom domain, the issuer is the app default FQDN "<appname>.<env-default-domain>",
// which is deterministic before the app is created (app name is fixed to "keycloak").
var effectiveHostname = empty(ssoHostname) ? 'keycloak.${environmentDefaultDomain}' : ssoHostname
var bindCustomDomain = enableCustomDomain && !empty(ssoHostname)

param postgresFqdn string
param postgresDatabase string
param postgresUsername string
param keycloakAdminUsername string

param cpu string = '1.0'
param memory string = '2.0Gi'

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: environmentName
}

resource managedCert 'Microsoft.App/managedEnvironments/managedCertificates@2024-03-01' = if (bindCustomDomain) {
  parent: environment
  name: replace(ssoHostname, '.', '-')
  location: location
  tags: tags
  properties: {
    subjectName: ssoHostname
    domainControlValidation: 'CNAME'
  }
}

var customDomains = bindCustomDomain ? [
  {
    name: ssoHostname
    certificateId: managedCert.id
    bindingType: 'SniEnabled'
  }
] : []

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
        customDomains: customDomains
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
