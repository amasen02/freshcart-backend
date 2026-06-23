param workloadName string
param environmentName string
param location string
param resourceTags object

var virtualNetworkName = '${workloadName}-${environmentName}-vnet'
var addressPrefix = '10.50.0.0/16'

resource virtualNetwork 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: virtualNetworkName
  location: location
  tags: resourceTags
  properties: {
    addressSpace: { addressPrefixes: [addressPrefix] }
    subnets: [
      {
        name: 'aks-system'
        properties: { addressPrefix: '10.50.0.0/22' }
      }
      {
        name: 'aks-user'
        properties: { addressPrefix: '10.50.4.0/22' }
      }
      {
        name: 'private-endpoints'
        properties: {
          addressPrefix: '10.50.8.0/24'
          privateEndpointNetworkPolicies: 'Disabled'
        }
      }
      {
        name: 'application-gateway'
        properties: { addressPrefix: '10.50.9.0/24' }
      }
    ]
  }
}

output virtualNetworkId string = virtualNetwork.id
output virtualNetworkName string = virtualNetwork.name
