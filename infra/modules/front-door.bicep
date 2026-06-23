param workloadName string
param environmentName string
param resourceTags object

var profileName = '${workloadName}-${environmentName}-fd'
var endpointName = '${workloadName}-${environmentName}'

resource frontDoorProfile 'Microsoft.Cdn/profiles@2024-09-01' = {
  name: profileName
  location: 'global'
  tags: resourceTags
  sku: { name: environmentName == 'prod' ? 'Premium_AzureFrontDoor' : 'Standard_AzureFrontDoor' }
  properties: { originResponseTimeoutSeconds: 60 }
}

resource frontDoorEndpoint 'Microsoft.Cdn/profiles/afdEndpoints@2024-09-01' = {
  parent: frontDoorProfile
  name: endpointName
  location: 'global'
  properties: { enabledState: 'Enabled' }
}

resource frontDoorWafPolicy 'Microsoft.Network/FrontDoorWebApplicationFirewallPolicies@2024-02-01' = {
  name: '${workloadName}${environmentName}waf'
  location: 'global'
  tags: resourceTags
  sku: { name: environmentName == 'prod' ? 'Premium_AzureFrontDoor' : 'Standard_AzureFrontDoor' }
  properties: {
    policySettings: {
      enabledState: 'Enabled'
      mode: environmentName == 'prod' ? 'Prevention' : 'Detection'
      requestBodyCheck: 'Enabled'
    }
    managedRules: {
      managedRuleSets: [
        { ruleSetType: 'Microsoft_DefaultRuleSet', ruleSetVersion: '2.1', ruleSetAction: 'Block' }
        { ruleSetType: 'Microsoft_BotManagerRuleSet', ruleSetVersion: '1.0' }
      ]
    }
  }
}

output endpointHostName string = frontDoorEndpoint.properties.hostName
output wafPolicyId string = frontDoorWafPolicy.id
