param workloadName string
param environmentName string
param location string
param resourceTags object

var workspaceName = '${workloadName}-${environmentName}-law'
var applicationInsightsName = '${workloadName}-${environmentName}-ai'

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2025-02-01' = {
  name: workspaceName
  location: location
  tags: resourceTags
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: environmentName == 'prod' ? 90 : 30
    features: { enableLogAccessUsingOnlyResourcePermissions: true }
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  tags: resourceTags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Flow_Type: 'Bluefield'
    WorkspaceResourceId: logAnalyticsWorkspace.id
    IngestionMode: 'LogAnalytics'
  }
}

output workspaceId string = logAnalyticsWorkspace.id
output workspaceName string = logAnalyticsWorkspace.name
output applicationInsightsName string = applicationInsights.name
output applicationInsightsConnectionString string = applicationInsights.properties.ConnectionString
