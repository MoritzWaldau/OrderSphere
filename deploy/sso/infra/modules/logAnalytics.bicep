// Log Analytics workspace — diagnostics destination for the Container Apps Environment.
@description('Azure region.')
param location string
@description('Resource name prefix.')
param namePrefix string
@description('Resource tags.')
param tags object

@description('Retention in days.')
param retentionInDays int = 30

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'law-${namePrefix}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: retentionInDays
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

output id string = workspace.id
output customerId string = workspace.properties.customerId
@secure()
output primarySharedKey string = workspace.listKeys().primarySharedKey
