param workloadName string
param environmentName string
param location string
param resourceTags object

var accountName = '${workloadName}-${environmentName}-cosmos'

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' = {
  name: accountName
  location: location
  tags: resourceTags
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: { defaultConsistencyLevel: 'Session' }
    enableAutomaticFailover: environmentName == 'prod'
    publicNetworkAccess: environmentName == 'prod' ? 'Disabled' : 'Enabled'
    capabilities: [{ name: 'EnableServerless' }]
    locations: [{ locationName: location, failoverPriority: 0, isZoneRedundant: environmentName == 'prod' }]
  }
}

resource notificationDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-08-15' = {
  parent: cosmosAccount
  name: 'notificationdb'
  properties: { resource: { id: 'notificationdb' } }
}

resource notificationContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
  parent: notificationDatabase
  name: 'notifications'
  properties: {
    resource: {
      id: 'notifications'
      partitionKey: { paths: ['/userId'], kind: 'Hash' }
      indexingPolicy: { indexingMode: 'consistent', automatic: true }
    }
  }
}

output accountName string = cosmosAccount.name
output endpoint string = cosmosAccount.properties.documentEndpoint
