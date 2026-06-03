// User-assigned managed identity for the Keycloak Container App.
// Used for ACR image pull and Key Vault secret reads (RBAC granted in those modules).
@description('Azure region.')
param location string
@description('Resource name prefix.')
param namePrefix string
@description('Resource tags.')
param tags object

resource uami 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${namePrefix}-keycloak'
  location: location
  tags: tags
}

output resourceId string = uami.id
output principalId string = uami.properties.principalId
output clientId string = uami.properties.clientId
