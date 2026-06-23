param workloadName string
param environmentName string
param location string
param resourceTags object

var storageAccountName = toLower('${workloadName}${environmentName}stg${uniqueString(resourceGroup().id)}')

resource storageAccount 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: storageAccountName
  location: location
  tags: resourceTags
  sku: { name: environmentName == 'prod' ? 'Standard_ZRS' : 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    supportsHttpsTrafficOnly: true
    publicNetworkAccess: environmentName == 'prod' ? 'Disabled' : 'Enabled'
    networkAcls: { defaultAction: environmentName == 'prod' ? 'Deny' : 'Allow', bypass: 'AzureServices' }
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2024-01-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    deleteRetentionPolicy: { enabled: true, days: 14 }
    containerDeleteRetentionPolicy: { enabled: true, days: 14 }
    isVersioningEnabled: true
    changeFeed: { enabled: true, retentionInDays: 90 }
  }
}

var blobContainerNames = ['product-images', 'invoices', 'scheduled-reports', 'uploads']

@batchSize(1)
resource blobContainers 'Microsoft.Storage/storageAccounts/blobServices/containers@2024-01-01' = [for containerName in blobContainerNames: {
  parent: blobService
  name: containerName
  properties: { publicAccess: 'None' }
}]

output storageAccountName string = storageAccount.name
output blobEndpoint string = storageAccount.properties.primaryEndpoints.blob
