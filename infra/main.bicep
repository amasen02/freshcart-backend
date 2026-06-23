// ---------------------------------------------------------------------------
// FreshCart — top-level deployment template.
// Subscription-scope deployment that creates the resource group and provisions
// every required Azure resource for one environment (dev / staging / prod).
//
// Deploy: az deployment sub create --location eastus \
//             --template-file infra/main.bicep \
//             --parameters @infra/env/dev.bicepparam
// ---------------------------------------------------------------------------

targetScope = 'subscription'

@description('Short environment identifier. Used in every resource name.')
@allowed(['dev', 'staging', 'prod'])
param environmentName string

@description('Azure region for all resources.')
param location string = 'eastus'

@description('Administrator login for Azure SQL Server.')
@secure()
param sqlAdministratorLogin string

@description('Administrator password for Azure SQL Server. Rotate via Key Vault in prod.')
@secure()
param sqlAdministratorPassword string

@description('Administrator login for Azure Database for PostgreSQL Flexible Server.')
@secure()
param postgresAdministratorLogin string

@description('Administrator password for Azure Database for PostgreSQL Flexible Server.')
@secure()
param postgresAdministratorPassword string

@description('Administrator login for Azure Database for MySQL Flexible Server.')
@secure()
param mysqlAdministratorLogin string

@description('Administrator password for Azure Database for MySQL Flexible Server.')
@secure()
param mysqlAdministratorPassword string

@description('Object id of the Entra ID group that should be granted Key Vault administrator.')
param keyVaultAdministratorObjectId string

var workloadName = 'freshcart'
var resourceGroupName = '${workloadName}-${environmentName}'
var resourceTags = {
  workload: workloadName
  environment: environmentName
  managedBy: 'bicep'
}

resource workloadResourceGroup 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
  tags: resourceTags
}

module networkModule 'modules/network.bicep' = {
  scope: workloadResourceGroup
  name: 'network'
  params: {
    workloadName: workloadName
    environmentName: environmentName
    location: location
    resourceTags: resourceTags
  }
}

module logAnalyticsModule 'modules/log-analytics.bicep' = {
  scope: workloadResourceGroup
  name: 'log-analytics'
  params: {
    workloadName: workloadName
    environmentName: environmentName
    location: location
    resourceTags: resourceTags
  }
}

module keyVaultModule 'modules/key-vault.bicep' = {
  scope: workloadResourceGroup
  name: 'key-vault'
  params: {
    workloadName: workloadName
    environmentName: environmentName
    location: location
    administratorObjectId: keyVaultAdministratorObjectId
    resourceTags: resourceTags
  }
}

module containerRegistryModule 'modules/acr.bicep' = {
  scope: workloadResourceGroup
  name: 'container-registry'
  params: {
    workloadName: workloadName
    environmentName: environmentName
    location: location
    resourceTags: resourceTags
  }
}

module aksClusterModule 'modules/aks.bicep' = {
  scope: workloadResourceGroup
  name: 'aks-cluster'
  params: {
    workloadName: workloadName
    environmentName: environmentName
    location: location
    logAnalyticsWorkspaceId: logAnalyticsModule.outputs.workspaceId
    resourceTags: resourceTags
  }
}

module sqlModule 'modules/sql.bicep' = {
  scope: workloadResourceGroup
  name: 'sql-server'
  params: {
    workloadName: workloadName
    environmentName: environmentName
    location: location
    administratorLogin: sqlAdministratorLogin
    administratorPassword: sqlAdministratorPassword
    resourceTags: resourceTags
  }
}

module postgresModule 'modules/postgres.bicep' = {
  scope: workloadResourceGroup
  name: 'postgres'
  params: {
    workloadName: workloadName
    environmentName: environmentName
    location: location
    administratorLogin: postgresAdministratorLogin
    administratorPassword: postgresAdministratorPassword
    resourceTags: resourceTags
  }
}

module mysqlModule 'modules/mysql.bicep' = {
  scope: workloadResourceGroup
  name: 'mysql'
  params: {
    workloadName: workloadName
    environmentName: environmentName
    location: location
    administratorLogin: mysqlAdministratorLogin
    administratorPassword: mysqlAdministratorPassword
    resourceTags: resourceTags
  }
}

module cosmosModule 'modules/cosmos.bicep' = {
  scope: workloadResourceGroup
  name: 'cosmos'
  params: {
    workloadName: workloadName
    environmentName: environmentName
    location: location
    resourceTags: resourceTags
  }
}

module redisModule 'modules/redis.bicep' = {
  scope: workloadResourceGroup
  name: 'redis'
  params: {
    workloadName: workloadName
    environmentName: environmentName
    location: location
    resourceTags: resourceTags
  }
}

module serviceBusModule 'modules/service-bus.bicep' = {
  scope: workloadResourceGroup
  name: 'service-bus'
  params: {
    workloadName: workloadName
    environmentName: environmentName
    location: location
    resourceTags: resourceTags
  }
}

module storageModule 'modules/storage.bicep' = {
  scope: workloadResourceGroup
  name: 'storage'
  params: {
    workloadName: workloadName
    environmentName: environmentName
    location: location
    resourceTags: resourceTags
  }
}

module frontDoorModule 'modules/front-door.bicep' = {
  scope: workloadResourceGroup
  name: 'front-door'
  params: {
    workloadName: workloadName
    environmentName: environmentName
    resourceTags: resourceTags
  }
}

output resourceGroupName string = workloadResourceGroup.name
output aksClusterName string = aksClusterModule.outputs.clusterName
output containerRegistryName string = containerRegistryModule.outputs.registryName
output keyVaultName string = keyVaultModule.outputs.keyVaultName
output applicationInsightsName string = logAnalyticsModule.outputs.applicationInsightsName
