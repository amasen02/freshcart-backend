param workloadName string
param environmentName string
param location string

@secure()
param administratorLogin string
@secure()
param administratorPassword string

param resourceTags object

var serverName = '${workloadName}-${environmentName}-mysql'

resource mysqlServer 'Microsoft.DBforMySQL/flexibleServers@2024-10-01-preview' = {
  name: serverName
  location: location
  tags: resourceTags
  sku: {
    name: environmentName == 'prod' ? 'Standard_D4ds_v5' : 'Standard_D2ds_v5'
    tier: 'GeneralPurpose'
  }
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorPassword
    version: '8.0.21'
    storage: {
      storageSizeGB: environmentName == 'prod' ? 128 : 32
      autoGrow: 'Enabled'
    }
    backup: {
      backupRetentionDays: environmentName == 'prod' ? 14 : 7
      geoRedundantBackup: environmentName == 'prod' ? 'Enabled' : 'Disabled'
    }
    highAvailability: { mode: environmentName == 'prod' ? 'ZoneRedundant' : 'Disabled' }
  }
}

resource reportingDatabase 'Microsoft.DBforMySQL/flexibleServers/databases@2024-10-01-preview' = {
  parent: mysqlServer
  name: 'freshcart_reporting'
  properties: { charset: 'utf8mb4', collation: 'utf8mb4_general_ci' }
}

output serverName string = mysqlServer.name
output serverFqdn string = mysqlServer.properties.fullyQualifiedDomainName
