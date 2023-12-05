@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@minLength(1)
@description('String representing the ID of the logged-in user. Get this using ')
param myUserId string

var tags = {
  'azd-env-name': environmentName
}

// resource token for naming each resource randomly, reliably
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var identityName = 'id${resourceToken}'

// ai deployments
resource openAI 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: 'openai-${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'S0'
  }
  kind: 'OpenAI'
  identity: {
    type: 'None'
    userAssignedIdentities: {}
  }
  properties: {
    allowedFqdnList: []
    apiProperties: {}
    customSubDomainName: 'openai-${resourceToken}'
    disableLocalAuth: false
    dynamicThrottlingEnabled: false
    encryption: null 
    locations: null 
    migrationToken: ''
    networkAcls: null 
    publicNetworkAccess: 'Enabled'
    restore: false
    restrictOutboundNetworkAccess: false
    userOwnedStorage: null 
  }
}

resource gpt 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  name: 'gpt${resourceToken}'
  parent: openAI
  sku: {
    name: 'Standard'
    capacity: 2
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-35-turbo'
      version: '0301'
    }
  }
  dependsOn: [
    openAI
  ]
}

resource embedding 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  name: 'text${resourceToken}'
  parent: openAI
  sku: {
    name: 'Standard'
    capacity: 5
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'text-embedding-ada-002'
      version: '2'
    }
  }
  dependsOn: [
    openAI
    gpt
  ]
}

// storage account for blobs and queues
resource storageaccount 'Microsoft.Storage/storageAccounts@2021-02-01' = {
  name: 'strg${resourceToken}'
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_RAGRS'
  }
  properties: {
    allowBlobPublicAccess: true
  }
}

// identity for the container apps
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
}

var principalId = identity.properties.principalId

// system role for setting up acr pull access
var strgBlbRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
var strgQueRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')

// storage role for blobs
resource blobRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageaccount.id, identity.id, strgBlbRole)
  scope: storageaccount
  properties: {
    roleDefinitionId: strgBlbRole
    principalType: 'ServicePrincipal'
    principalId: principalId
  }
}

resource blobRoleAssignmentForMe 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageaccount.id, myUserId, strgBlbRole)
  scope: storageaccount
  properties: {
    roleDefinitionId: strgBlbRole
    principalType: 'User'
    principalId: myUserId
  }
}

// storage role for queues
resource queueRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageaccount.id, identity.id, strgQueRole)
  scope: storageaccount
  properties: {
    roleDefinitionId: strgQueRole
    principalType: 'ServicePrincipal'
    principalId: principalId
  }
}
resource queueRoleAssignmentForMe 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageaccount.id, myUserId, strgQueRole)
  scope: storageaccount
  properties: {
    roleDefinitionId: strgQueRole
    principalType: 'User'
    principalId: myUserId
  }
}

// output environment variables
output AZUREOPENAI_ENDPOINT string = openAI.properties.endpoint
output AZUREOPENAI_KEY string = openAI.listKeys().key1
output AI_GPT_DEPLOYMENT_NAME string = 'gpt${resourceToken}'
output AI_TEXT_DEPLOYMENT_NAME string = 'text${resourceToken}'
output AZURE_CLIENT_ID string = identity.properties.clientId
output AZURE_BLOB_ENDPOINT string = 'https://${storageaccount.name}.blob.core.windows.net/'
output AZURE_QUEUE_ENDPOINT string = 'https://${storageaccount.name}.queue.core.windows.net/'
