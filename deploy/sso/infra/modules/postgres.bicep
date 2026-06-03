// PostgreSQL Flexible Server (private access) + the "keycloak" database.
@description('Azure region.')
param location string
@description('Resource name prefix.')
param namePrefix string
@description('Resource tags.')
param tags object
@description('Delegated subnet for private access.')
param delegatedSubnetId string
@description('Private DNS zone resource ID.')
param privateDnsZoneId string

param administratorLogin string
@secure()
param administratorPassword string

param postgresVersion string = '16'
param skuName string = 'Standard_B1ms'
param skuTier string = 'Burstable'
param storageSizeGB int = 32

var databaseName = 'keycloak'

resource server 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: 'psql-${namePrefix}'
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuTier
  }
  properties: {
    version: postgresVersion
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorPassword
    storage: {
      storageSizeGB: storageSizeGB
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
    network: {
      delegatedSubnetResourceId: delegatedSubnetId
      privateDnsZoneArmResourceId: privateDnsZoneId
    }
  }
}

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: server
  name: databaseName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

output fqdn string = server.properties.fullyQualifiedDomainName
output databaseName string = databaseName
output serverName string = server.name
