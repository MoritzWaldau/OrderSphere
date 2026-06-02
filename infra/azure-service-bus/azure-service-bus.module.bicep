@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param sku string = 'Standard'

resource azure_service_bus 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: take('azureservicebus-${uniqueString(resourceGroup().id)}', 50)
  location: location
  properties: {
    disableLocalAuth: true
    publicNetworkAccess: 'Enabled'
  }
  sku: {
    name: sku
  }
  tags: {
    'aspire-resource-name': 'azure-service-bus'
  }
}

resource orders 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  name: 'orders'
  properties: {
    maxDeliveryCount: 10
  }
  parent: azure_service_bus
}

resource notification_orders 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  name: 'notification-orders'
  properties: {
    maxDeliveryCount: 5
  }
  parent: azure_service_bus
}

resource payment_requests 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  name: 'payment-requests'
  properties: {
    maxDeliveryCount: 10
  }
  parent: azure_service_bus
}

resource payment_results 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  name: 'payment-results'
  properties: {
    maxDeliveryCount: 5
  }
  parent: azure_service_bus
}

resource realtime_notifications 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  name: 'realtime-notifications'
  properties: {
    maxDeliveryCount: 5
  }
  parent: azure_service_bus
}

resource webhook_events 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  name: 'webhook-events'
  properties: {
    maxDeliveryCount: 5
  }
  parent: azure_service_bus
}

output serviceBusEndpoint string = azure_service_bus.properties.serviceBusEndpoint

output serviceBusHostName string = split(replace(azure_service_bus.properties.serviceBusEndpoint, 'https://', ''), ':')[0]

output name string = azure_service_bus.name

output id string = azure_service_bus.id