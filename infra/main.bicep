targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention, the name of the resource group for your application will use this name, prefixed with rg-')
param environmentName string

@minLength(1)
@description('The location used for all deployed resources')
param location string

@description('Id of the user or app to assign application roles')
param principalId string = ''

@secure()
param bff_client_secret string
@secure()
param keycloak_admin_password string
param keycloak_realm_authority string
@secure()
param notification_worker_secret string
@secure()
param ordering_worker_secret string
param payment_bypass_providers string
@secure()
param payment_worker_secret string

var tags = {
  'azd-env-name': environmentName
}

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}
module resources 'resources.bicep' = {
  scope: rg
  name: 'resources'
  params: {
    location: location
    tags: tags
    principalId: principalId
  }
}

module azure_service_bus 'azure-service-bus/azure-service-bus.module.bicep' = {
  name: 'azure-service-bus'
  scope: rg
  params: {
    location: location
  }
}
module azure_service_bus_roles 'azure-service-bus-roles/azure-service-bus-roles.module.bicep' = {
  name: 'azure-service-bus-roles'
  scope: rg
  params: {
    azure_service_bus_outputs_name: azure_service_bus.outputs.name
    location: location
    principalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    principalType: 'ServicePrincipal'
  }
}
module ordersphere_kv 'ordersphere-kv/ordersphere-kv.module.bicep' = {
  name: 'ordersphere-kv'
  scope: rg
  params: {
    location: location
  }
}
module ordersphere_kv_roles 'ordersphere-kv-roles/ordersphere-kv-roles.module.bicep' = {
  name: 'ordersphere-kv-roles'
  scope: rg
  params: {
    location: location
    ordersphere_kv_outputs_name: ordersphere_kv.outputs.name
    principalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    principalType: 'ServicePrincipal'
  }
}
module postgres 'postgres/postgres.module.bicep' = {
  name: 'postgres'
  scope: rg
  params: {
    location: location
  }
}
module postgres_roles 'postgres-roles/postgres-roles.module.bicep' = {
  name: 'postgres-roles'
  scope: rg
  params: {
    location: location
    postgres_outputs_name: postgres.outputs.name
    principalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    principalName: resources.outputs.MANAGED_IDENTITY_NAME
    principalType: 'ServicePrincipal'
  }
}
module redis 'redis/redis.module.bicep' = {
  name: 'redis'
  scope: rg
  params: {
    location: location
  }
}
module redis_roles 'redis-roles/redis-roles.module.bicep' = {
  name: 'redis-roles'
  scope: rg
  params: {
    location: location
    principalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    redis_outputs_name: redis.outputs.name
  }
}

output MANAGED_IDENTITY_CLIENT_ID string = resources.outputs.MANAGED_IDENTITY_CLIENT_ID
output MANAGED_IDENTITY_NAME string = resources.outputs.MANAGED_IDENTITY_NAME
output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = resources.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_NAME
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = resources.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = resources.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID
output AZURE_CONTAINER_REGISTRY_NAME string = resources.outputs.AZURE_CONTAINER_REGISTRY_NAME
output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_NAME
output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN
output AZURE_SERVICE_BUS_SERVICEBUSENDPOINT string = azure_service_bus.outputs.serviceBusEndpoint
output AZURE_SERVICE_BUS_SERVICEBUSHOSTNAME string = azure_service_bus.outputs.serviceBusHostName
output ORDERSPHERE_KV_VAULTURI string = ordersphere_kv.outputs.vaultUri
output POSTGRES_CONNECTIONSTRING string = postgres.outputs.connectionString
output POSTGRES_HOSTNAME string = postgres.outputs.hostName
output REDIS_CONNECTIONSTRING string = redis.outputs.connectionString
output REDIS_HOSTNAME string = redis.outputs.hostName
