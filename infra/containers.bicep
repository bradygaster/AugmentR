@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('CPU cores allocated to a single container instance, e.g., 0.5')
param containerCpuCoreCount string = '0.5'

@description('Memory allocated to a single container instance, e.g., 1Gi')
param containerMemory string = '1.0Gi'

var tags = {
  'azd-env-name': environmentName
}

// resource token for naming each resource randomly, reliably
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var identityName = 'id${resourceToken}'

// storage account for blobs and queues
resource storageaccount 'Microsoft.Storage/storageAccounts@2021-02-01' existing = {
  name: 'strg${resourceToken}'
}

// log analytics 
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2021-12-01-preview' = {
  name: 'logs${resourceToken}'
  location: location
  tags: tags
  properties: any({
    retentionInDays: 30
    features: {
      searchVersion: 1
    }
    sku: {
      name: 'PerGB2018'
    }
  })
}

// application insights
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'ai${resourceToken}'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

// the container apps environment
resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-04-01-preview' = {
  name: 'acae${resourceToken}'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

// the container registry
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2022-02-01-preview' = {
  name: 'cr${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
    anonymousPullEnabled: false
    dataEndpointEnabled: false
    encryption: {
      status: 'disabled'
    }
    networkRuleBypassOptions: 'AzureServices'
    publicNetworkAccess: 'Enabled'
    zoneRedundancy: 'Disabled'
  }
}

// redis - azure container apps service
resource redis 'Microsoft.App/containerApps@2023-04-01-preview' = {
  name: 'redis'
  location: location
  tags: tags
  identity: {
    type: 'None'
    userAssignedIdentities: null
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 6379
        transport: 'tcp'
      }
      dapr: { enabled: false }
      service: { type: 'redis' }
    }
    template: {
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

// postgres - azure container apps service
resource postgres 'Microsoft.App/containerApps@2023-04-01-preview' = {
  name: 'postgres'
  location: location
  tags: tags
  identity: {
    type: 'None'
    userAssignedIdentities: null
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 5432
        transport: 'tcp'
      }
      dapr: { enabled: false }
      service: { type: 'postgres' }
    }
    template: {
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

// qdrant (from Dockerhub, not an add-on)
resource qdrant 'Microsoft.App/containerApps@2023-04-01-preview' = {
  name: 'qdrant'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identity.id}' : {}}
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 6333
        transport: 'tcp'
      }
      dapr: { enabled: false }
    }
    template: {
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
      containers: [
        {
          image: 'qdrant/qdrant'
          name: 'qdrant'
          resources: {
            cpu: json(containerCpuCoreCount)
            memory: containerMemory
          }
        }
      ]
    }
  }
}

// identity for the container apps
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: identityName
}

var principalId = identity.properties.principalId

var acrPullRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')

// allow acr pulls to the identity used for the aca's
resource aksAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: containerRegistry // Use when specifying a scope that is different than the deployment scope
  name: guid(subscription().id, identity.id, acrPullRole)
  properties: {
    roleDefinitionId: acrPullRole
    principalType: 'ServicePrincipal'
    principalId: principalId
  }
}

// output environment variables
output AZURE_CONTAINER_REGISTRY string = containerRegistry.properties.loginServer
