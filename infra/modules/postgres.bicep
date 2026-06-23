param workloadName string
param environmentName string
param location string

@secure()
param administratorLogin string
@secure()
param administratorPassword string

param resourceTags object

var serverName = '${workloadName}-${environmentName}-pg'
var skuName = environmentName == 'prod' ? 'Standard_D4ds_v5' : 'Standard_D2ds_v5'
var skuTier = 'GeneralPurpose'

resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: serverName
  location: location
  tags: resourceTags
  sku: { name: skuName, tier: skuTier }
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorPassword
    version: '16'
    storage: { storageSizeGB: environmentName == 'prod' ? 256 : 64, autoGrow: 'Enabled' }
    backup: {
      backupRetentionDays: environmentName == 'prod' ? 35 : 7
      geoRedundantBackup: environmentName == 'prod' ? 'Enabled' : 'Disabled'
    }
    highAvailability: { mode: environmentName == 'prod' ? 'ZoneRedundant' : 'Disabled' }
  }
}

var databaseNames = ['catalogdb', 'basketdb']

@batchSize(1)
resource databases 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = [for databaseName in databaseNames: {
  parent: postgresServer
  name: databaseName
  properties: { charset: 'utf8', collation: 'en_US.utf8' }
}]

output serverName string = postgresServer.name
output serverFqdn string = postgresServer.properties.fullyQualifiedDomainName
