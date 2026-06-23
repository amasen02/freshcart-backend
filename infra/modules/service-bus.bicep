param workloadName string
param environmentName string
param location string
param resourceTags object

var namespaceName = '${workloadName}-${environmentName}-sb'

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: namespaceName
  location: location
  tags: resourceTags
  sku: {
    name: environmentName == 'prod' ? 'Premium' : 'Standard'
    tier: environmentName == 'prod' ? 'Premium' : 'Standard'
    capacity: environmentName == 'prod' ? 1 : 0
  }
  properties: {
    publicNetworkAccess: environmentName == 'prod' ? 'Disabled' : 'Enabled'
    minimumTlsVersion: '1.2'
    zoneRedundant: environmentName == 'prod'
  }
}

var topicNames = [
  'order-events'
  'payment-events'
  'delivery-events'
  'notification-events'
]

@batchSize(1)
resource topics 'Microsoft.ServiceBus/namespaces/topics@2024-01-01' = [for topicName in topicNames: {
  parent: serviceBusNamespace
  name: topicName
  properties: {
    defaultMessageTimeToLive: 'P14D'
    enablePartitioning: false
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'
  }
}]

output namespaceName string = serviceBusNamespace.name
output namespaceFqdn string = '${serviceBusNamespace.name}.servicebus.windows.net'
