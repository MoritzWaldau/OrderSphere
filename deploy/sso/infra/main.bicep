// ===========================================================================
// OrderSphere SSO — central Keycloak identity provider (Azure DEV)
// Resource-group scoped. Deploy with:
//   az deployment group create -g rg-sso-dev -f infra/main.bicep -p infra/params/dev.bicepparam
// ===========================================================================
targetScope = 'resourceGroup'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Short prefix for resource names, e.g. "ssodev".')
@minLength(3)
@maxLength(12)
param namePrefix string

@description('Resource tags applied to every resource.')
param tags object = {
  workload: 'ordersphere-sso'
  environment: 'dev'
  managedBy: 'bicep'
}

@description('Fully-qualified custom hostname for the issuer, e.g. "sso.dev.example.com". Leave empty to use the Container Apps default FQDN (no domain required).')
param ssoHostname string = ''

@description('Full container image reference for Keycloak (built and pushed by the pipeline), e.g. "ssodevacr.azurecr.io/keycloak:<tag>".')
param keycloakImage string

@description('PostgreSQL administrator login.')
param postgresAdminUsername string = 'kcadmin'

@secure()
@description('PostgreSQL administrator password.')
param postgresAdminPassword string

@secure()
@description('Keycloak bootstrap admin password (only used on first start).')
param keycloakAdminPassword string

@description('Keycloak bootstrap admin username (only used on first start).')
param keycloakAdminUsername string = 'admin'

// --- Networking -----------------------------------------------------------
module network 'modules/network.bicep' = {
  name: 'network'
  params: {
    location: location
    namePrefix: namePrefix
    tags: tags
  }
}

// --- Observability ---------------------------------------------------------
module logs 'modules/logAnalytics.bicep' = {
  name: 'logAnalytics'
  params: {
    location: location
    namePrefix: namePrefix
    tags: tags
  }
}

// --- Identity (consumed by ACR pull + Key Vault read) ----------------------
module identity 'modules/identity.bicep' = {
  name: 'identity'
  params: {
    location: location
    namePrefix: namePrefix
    tags: tags
  }
}

// --- Container Registry (custom Keycloak image) ----------------------------
module acr 'modules/acr.bicep' = {
  name: 'acr'
  params: {
    location: location
    namePrefix: namePrefix
    tags: tags
    pullPrincipalId: identity.outputs.principalId
  }
}

// --- Key Vault (admin + db secrets) ----------------------------------------
module keyvault 'modules/keyvault.bicep' = {
  name: 'keyvault'
  params: {
    location: location
    namePrefix: namePrefix
    tags: tags
    readerPrincipalId: identity.outputs.principalId
    postgresAdminPassword: postgresAdminPassword
    keycloakAdminPassword: keycloakAdminPassword
  }
}

// --- PostgreSQL Flexible Server (Keycloak persistence) ---------------------
module postgres 'modules/postgres.bicep' = {
  name: 'postgres'
  params: {
    location: location
    namePrefix: namePrefix
    tags: tags
    delegatedSubnetId: network.outputs.postgresSubnetId
    privateDnsZoneId: network.outputs.postgresPrivateDnsZoneId
    administratorLogin: postgresAdminUsername
    administratorPassword: postgresAdminPassword
  }
}

// --- Container Apps Environment (VNet-injected) ----------------------------
module containerEnv 'modules/containerEnv.bicep' = {
  name: 'containerEnv'
  params: {
    location: location
    namePrefix: namePrefix
    tags: tags
    infrastructureSubnetId: network.outputs.caeSubnetId
    logAnalyticsCustomerId: logs.outputs.customerId
    logAnalyticsSharedKey: logs.outputs.primarySharedKey
  }
}

// --- Keycloak Container App ------------------------------------------------
module keycloak 'modules/keycloakApp.bicep' = {
  name: 'keycloakApp'
  params: {
    location: location
    tags: tags
    environmentId: containerEnv.outputs.environmentId
    environmentDefaultDomain: containerEnv.outputs.defaultDomain
    image: keycloakImage
    userAssignedIdentityId: identity.outputs.resourceId
    acrLoginServer: acr.outputs.loginServer
    keyVaultUri: keyvault.outputs.vaultUri
    ssoHostname: ssoHostname
    postgresFqdn: postgres.outputs.fqdn
    postgresDatabase: postgres.outputs.databaseName
    postgresUsername: postgresAdminUsername
    keycloakAdminUsername: keycloakAdminUsername
  }
}

output keycloakFqdn string = keycloak.outputs.fqdn
output customDomainVerificationId string = keycloak.outputs.customDomainVerificationId
output acrLoginServer string = acr.outputs.loginServer
output issuerUrl string = keycloak.outputs.issuerUrl
@description('Use this value for the OrderSphere keycloak-realm-authority parameter.')
output keycloakRealmAuthority string = keycloak.outputs.issuerUrl
