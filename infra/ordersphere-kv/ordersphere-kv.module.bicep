@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource ordersphere_kv 'Microsoft.KeyVault/vaults@2024-11-01' = {
  name: take('orderspherekv-${uniqueString(resourceGroup().id)}', 24)
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
  }
  tags: {
    'aspire-resource-name': 'ordersphere-kv'
  }
}

output vaultUri string = ordersphere_kv.properties.vaultUri

output name string = ordersphere_kv.name

output id string = ordersphere_kv.id