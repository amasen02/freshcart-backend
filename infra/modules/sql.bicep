param workloadName string
param environmentName string
param location string

@secure()
param administratorLogin string
@secure()
param administratorPassword string

param resourceTags object

var serverName = '${workloadName}-${environmentName}-sql'

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: serverName
  location: location
  tags: resourceTags
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: environmentName == 'prod' ? 'Disabled' : 'Enabled'
  }
}

var databaseNames = ['identitydb', 'orderingdb', 'inventorydb', 'paymentreaddb']
var skuName = environmentName == 'prod' ? 'S3' : 'S2'
var skuTier = 'Standard'

@batchSize(1)
resource sqlDatabases 'Microsoft.Sql/servers/databases@2023-08-01-preview' = [for databaseName in databaseNames: {
  parent: sqlServer
  name: databaseName
  location: location
  tags: resourceTags
  sku: { name: skuName, tier: skuTier }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: environmentName == 'prod'
  }
}]

resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = if (environmentName != 'prod') {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: { startIpAddress: '0.0.0.0', endIpAddress: '0.0.0.0' }
}

output sqlServerName string = sqlServer.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
