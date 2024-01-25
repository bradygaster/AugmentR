@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@minLength(1)
@description('String representing the ID of the logged-in user. Get this using')
param principalId string

@description('Name of the keyvault to store secrets')
param keyVaultName string

@description('Name of the openai key secret in the keyvault')
param openAIKeyName string

@description('Name of the identity to create')
param identityName string

@description('Name of the storage account to create')
param storageName string

@description('Name of the openai deployment')
param openAIName string

var tags = {
  'azd-env-name': environmentName
}

// resource token for naming each resource randomly, reliably
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

// storage account for blobs and queues
module storage '../core/storage/storage-account.bicep' = {
  name: 'storage'
  params: {
    name: storageName
    location: location
    tags: tags
    sku: {
      name: 'Standard_RAGRS'
    }
    kind: 'StorageV2'
    allowBlobPublicAccess: false
  }
}

// identity for the container apps
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
  tags: tags
}

// system role for setting up acr pull access
var strgBlbRole = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
var strgQueRole = '974c5e8b-45b9-4653-ba55-5f855dd0fb88'

// storage role for blobs
module blobRoleAssignment '../core/security/role.bicep' = {
  name: 'blob-role-identity'
  params: {
    roleDefinitionId: strgBlbRole
    principalType: 'ServicePrincipal'
    principalId: identity.properties.principalId
  }
}

module blobRoleAssignmentForMe '../core/security/role.bicep' = {
  name: 'blob-role-user'
  params: {
    roleDefinitionId: strgBlbRole
    principalType: 'User'
    principalId: principalId
  }
}

// storage role for queues
module queueRoleAssignment '../core/security/role.bicep' = {
  name: 'queue-role-identity'
  params: {
    roleDefinitionId: strgQueRole
    principalType: 'ServicePrincipal'
    principalId: identity.properties.principalId
  }
}

module queueRoleAssignmentForMe '../core/security/role.bicep' = {
  name: 'queue-role-user'
  params: {
    roleDefinitionId: strgQueRole
    principalType: 'User'
    principalId: principalId
  }
}

// create secret to store openai api key
module openAIKey '../core/security/keyvault-secret.bicep' = {
  name: 'openai-key'
  params: {
    name: openAIKeyName
    keyVaultName: keyVaultName
    secretValue: listKeys(resourceId(subscription().subscriptionId, resourceGroup().name, 'Microsoft.CognitiveServices/accounts', openAIName), '2023-05-01').key1
  }
}

// output environment variables
output principalId string = identity.properties.principalId
output AI_GPT_DEPLOYMENT_NAME string = 'gpt${resourceToken}'
output AI_TEXT_DEPLOYMENT_NAME string = 'text${resourceToken}'
output AZURE_CLIENT_ID string = identity.properties.clientId
output AZURE_BLOB_ENDPOINT string = 'https://${storage.outputs.name}.blob.core.windows.net/'
output AZURE_QUEUE_ENDPOINT string = 'https://${storage.outputs.name}.queue.core.windows.net/'
output identityName string = identity.name
