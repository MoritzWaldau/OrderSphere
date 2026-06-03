// VNet with delegated subnets for the Container Apps Environment and the
// PostgreSQL Flexible Server, plus the private DNS zone for Postgres.
@description('Azure region.')
param location string
@description('Resource name prefix.')
param namePrefix string
@description('Resource tags.')
param tags object

param vnetAddressPrefix string = '10.20.0.0/16'
param caeSubnetPrefix string = '10.20.0.0/23'
param postgresSubnetPrefix string = '10.20.2.0/24'

var postgresPrivateDnsZoneName = '${namePrefix}.private.postgres.database.azure.com'

resource vnet 'Microsoft.Network/virtualNetworks@2023-11-01' = {
  name: 'vnet-${namePrefix}'
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [vnetAddressPrefix]
    }
    subnets: [
      {
        name: 'snet-cae'
        properties: {
          addressPrefix: caeSubnetPrefix
          delegations: [
            {
              name: 'cae-delegation'
              properties: {
                serviceName: 'Microsoft.App/environments'
              }
            }
          ]
        }
      }
      {
        name: 'snet-postgres'
        properties: {
          addressPrefix: postgresSubnetPrefix
          delegations: [
            {
              name: 'postgres-delegation'
              properties: {
                serviceName: 'Microsoft.DBforPostgreSQL/flexibleServers'
              }
            }
          ]
        }
      }
    ]
  }
}

resource postgresDnsZone 'Microsoft.Network/privateDnsZones@2024-06-01' = {
  name: postgresPrivateDnsZoneName
  location: 'global'
  tags: tags
}

resource postgresDnsLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2024-06-01' = {
  parent: postgresDnsZone
  name: 'link-${namePrefix}'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnet.id
    }
  }
}

output caeSubnetId string = vnet.properties.subnets[0].id
output postgresSubnetId string = vnet.properties.subnets[1].id
output postgresPrivateDnsZoneId string = postgresDnsZone.id
