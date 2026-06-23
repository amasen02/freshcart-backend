param workloadName string
param environmentName string
param location string
param resourceTags object

var registryName = toLower('${workloadName}${environmentName}acr')
var registrySku = environmentName == 'prod' ? 'Premium' : 'Standard'

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: registryName
  location: location
  tags: resourceTags
  sku: { name: registrySku }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: environmentName == 'prod' ? 'Disabled' : 'Enabled'
    zoneRedundancy: registrySku == 'Premium' ? 'Enabled' : 'Disabled'
    encryption: { status: 'enabled' }
  }
}

output registryName string = containerRegistry.name
output loginServer string = containerRegistry.properties.loginServer
