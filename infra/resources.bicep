@description('The location used for all deployed resources')
param location string = resourceGroup().location

@description('Tags that will be applied to all resources')
param tags object = {}

@description('String representing the ID of the logged-in user')
param principalId string = ''

@description('Name of the key vault used by the app')
param keyvaultName string = ''

@description('Name of the openai key secret in the keyvault')
param openAIKeyName string = 'AZURE-OPEN-AI-KEY'

@description('Name of the openai key secret in the keyvault')
param openAIName string

var resourceToken = uniqueString(resourceGroup().id)

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'mi-${resourceToken}'
  location: location
  tags: tags
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: replace('acr-${resourceToken}', '-', '')
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
  tags: tags
}

resource caeMiRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(containerRegistry.id, managedIdentity.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d'))
  scope: containerRegistry
  properties: {
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId:  subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  }
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'law-${resourceToken}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
  tags: tags
}

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: 'cae-${resourceToken}'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
  }
  tags: tags
}

resource historydb 'Microsoft.App/containerApps@2023-05-02-preview' = {
  name: 'historydb'
  location: location
  properties: {
    environmentId: containerAppEnvironment.id
    configuration: {
      service: {
        type: 'postgres'
      }
    }
    template: {
      containers: [
        {
          image: 'postgres'
          name: 'postgres'
        }
      ]
    }
  }
  tags: union(tags, {'aspire-resource-name': 'historydb'})
}

resource pubsub 'Microsoft.App/containerApps@2023-05-02-preview' = {
  name: 'pubsub'
  location: location
  properties: {
    environmentId: containerAppEnvironment.id
    configuration: {
      service: {
        type: 'redis'
      }
    }
    template: {
      containers: [
        {
          image: 'redis'
          name: 'redis'
        }
      ]
    }
  }
  tags: union(tags, {'aspire-resource-name': 'pubsub'})
}

resource qdrant 'Microsoft.App/containerApps@2023-05-02-preview' = {
  name: 'qdrant'
  location: location
  properties: {
    environmentId: containerAppEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      
      ingress: {
        external: false
        targetPort: 6333
        transport: 'http'
        allowInsecure: true
      }
      
    }
    template: {
      containers: [
        {
          image: 'qdrant/qdrant:latest'
          name: 'qdrant'
        }
      ]
    }
  }
  tags: union(tags, {'aspire-resource-name': 'qdrant'})
}

resource storage 'Microsoft.Storage/storageAccounts@2022-05-01' = {
  name: replace('storage-${resourceToken}', '-', '')
  location: location
  kind: 'Storage'
  sku: {
    name: 'Standard_GRS'
  }
  tags: union(tags, {'aspire-resource-name': 'storage'})

  resource blobs 'blobServices@2022-05-01' = {
    name: 'default'
  }
}

// storage role assignments for the logged-in user so they can dev AND for the app

var strgBlbRole = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
var strgQueRole = '974c5e8b-45b9-4653-ba55-5f855dd0fb88'

// storage role for blobs
module blobRoleAssignment 'core/security/role.bicep' = {
    name: 'blob-role-app'
    params: {
        roleDefinitionId: strgBlbRole
        principalType: 'ServicePrincipal'
        principalId: managedIdentity.properties.principalId
    }
}

module blobRoleAssignmentForMe 'core/security/role.bicep' = {
    name: 'blob-role-user'
    params: {
        roleDefinitionId: strgBlbRole
        principalType: 'User'
        principalId: principalId
    }
}

// storage role for queues
module queueRoleAssignment 'core/security/role.bicep' = {
    name: 'queue-role-app'
    params: {
        roleDefinitionId: strgQueRole
        principalType: 'ServicePrincipal'
        principalId: managedIdentity.properties.principalId
    }
}

module queueRoleAssignmentForMe 'core/security/role.bicep' = {
    name: 'queue-role-user'
    params: {
        roleDefinitionId: strgQueRole
        principalType: 'User'
        principalId: principalId
    }
}

// give the container apps access to the keyvault
module keyvaultRole 'core/security/keyvault-access.bicep' = {
    name: 'keyvaultRole'
    params: {
        keyVaultName: keyvaultName
        principalId: managedIdentity.properties.principalId
    }
}

// create secret to store openai api key
module openAIKey 'core/security/keyvault-secret.bicep' = {
    name: 'openai-key'
    params: {
        name: openAIKeyName
        keyVaultName: keyvaultName
        secretValue: listKeys(resourceId(subscription().subscriptionId, resourceGroup().name, 'Microsoft.CognitiveServices/accounts', openAIName), '2023-05-01').key1
    }
}

output MANAGED_IDENTITY_CLIENT_ID string = managedIdentity.properties.clientId
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = containerRegistry.properties.loginServer
output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = managedIdentity.id
output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = containerAppEnvironment.id
output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = containerAppEnvironment.properties.defaultDomain
output SERVICE_BINDING_AZUREBLOBS_ENDPOINT string = storage.properties.primaryEndpoints.blob
output SERVICE_BINDING_AZUREQUEUES_ENDPOINT string = storage.properties.primaryEndpoints.queue
