@description('Workload name prefix.')
param workloadName string

@description('Environment short code (dev/staging/prod).')
param environmentName string

@description('Azure region.')
param location string

@description('Log Analytics workspace id used by AKS Container Insights.')
param logAnalyticsWorkspaceId string

@description('Resource tags applied to every resource.')
param resourceTags object

var clusterName = '${workloadName}-${environmentName}-aks'
var nodePoolMinCount = environmentName == 'prod' ? 3 : 2
var nodePoolMaxCount = environmentName == 'prod' ? 10 : 5
var systemNodeVmSize = 'Standard_D4s_v5'
var userNodeVmSize = environmentName == 'prod' ? 'Standard_D8s_v5' : 'Standard_D4s_v5'

resource aksCluster 'Microsoft.ContainerService/managedClusters@2024-09-01' = {
  name: clusterName
  location: location
  tags: resourceTags
  identity: { type: 'SystemAssigned' }
  sku: { name: 'Base', tier: environmentName == 'prod' ? 'Standard' : 'Free' }
  properties: {
    kubernetesVersion: '1.30'
    dnsPrefix: clusterName
    enableRBAC: true
    aadProfile: { managed: true, enableAzureRBAC: true }
    oidcIssuerProfile: { enabled: true }
    securityProfile: { workloadIdentity: { enabled: true } }
    apiServerAccessProfile: { enablePrivateCluster: environmentName == 'prod' }
    agentPoolProfiles: [
      {
        name: 'systempool'
        mode: 'System'
        count: 2
        minCount: 2
        maxCount: 4
        enableAutoScaling: true
        vmSize: systemNodeVmSize
        osType: 'Linux'
        osDiskSizeGB: 64
        type: 'VirtualMachineScaleSets'
        availabilityZones: ['1', '2', '3']
      }
      {
        name: 'userpool'
        mode: 'User'
        count: nodePoolMinCount
        minCount: nodePoolMinCount
        maxCount: nodePoolMaxCount
        enableAutoScaling: true
        vmSize: userNodeVmSize
        osType: 'Linux'
        osDiskSizeGB: 128
        type: 'VirtualMachineScaleSets'
        availabilityZones: ['1', '2', '3']
        nodeLabels: { workload: 'freshcart-services' }
      }
    ]
    networkProfile: {
      networkPlugin: 'azure'
      networkPolicy: 'cilium'
      networkDataplane: 'cilium'
      loadBalancerSku: 'standard'
    }
    addonProfiles: {
      omsagent: {
        enabled: true
        config: { logAnalyticsWorkspaceResourceID: logAnalyticsWorkspaceId }
      }
      azureKeyvaultSecretsProvider: {
        enabled: true
        config: { enableSecretRotation: 'true', rotationPollInterval: '2m' }
      }
    }
    autoUpgradeProfile: { upgradeChannel: environmentName == 'prod' ? 'stable' : 'patch' }
  }
}

output clusterName string = aksCluster.name
output clusterPrincipalId string = aksCluster.identity.principalId
output oidcIssuerUrl string = aksCluster.properties.oidcIssuerProfile.issuerURL
