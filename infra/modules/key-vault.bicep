param workloadName string
param environmentName string
param location string
param administratorObjectId string
param resourceTags object

var keyVaultName = '${workloadName}-${environmentName}-kv'

resource keyVault 'Microsoft.KeyVault/vaults@2024-04-01-preview' = {
  name: keyVaultName
  location: location
  tags: resourceTags
  properties: {
    tenantId: subscription().tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: environmentName == 'prod' ? true : null
    publicNetworkAccess: environmentName == 'prod' ? 'Disabled' : 'Enabled'
    networkAcls: { defaultAction: 'Allow', bypass: 'AzureServices' }
  }
}

var keyVaultAdministratorRoleId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '00482a5a-887f-4fb3-b363-3b7fe8e74483')

resource administratorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, administratorObjectId, keyVaultAdministratorRoleId)
  properties: {
    roleDefinitionId: keyVaultAdministratorRoleId
    principalId: administratorObjectId
    principalType: 'Group'
  }
}

output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
