param workloadName string
param environmentName string
param location string
param resourceTags object

var cacheName = '${workloadName}-${environmentName}-redis'

resource redisCache 'Microsoft.Cache/redis@2024-11-01' = {
  name: cacheName
  location: location
  tags: resourceTags
  properties: {
    sku: {
      name: environmentName == 'prod' ? 'Standard' : 'Basic'
      family: 'C'
      capacity: environmentName == 'prod' ? 2 : 1
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    publicNetworkAccess: environmentName == 'prod' ? 'Disabled' : 'Enabled'
    redisConfiguration: {
      'maxmemory-policy': 'allkeys-lru'
    }
  }
}

output cacheName string = redisCache.name
output hostName string = redisCache.properties.hostName
