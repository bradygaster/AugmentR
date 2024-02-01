targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention, the name of the resource group for your application will use this name, prefixed with rg-')
param environmentName string

@minLength(1)
@description('The location used for all deployed resources')
param location string

@description('String representing the ID of the logged-in user. Get this using ')
param principalId string = ''

@description('Name of the openai key secret in the keyvault')
param openAIKeyName string = 'AZURE-OPEN-AI-KEY'

var tags = {
  'azd-env-name': environmentName
}

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module ai 'ai.bicep' = {
  scope: rg
  name: 'ai'
  params: {
    location: location
    tags: tags
    openAIKeyName: openAIKeyName
  }
}

module keyvault 'keyvault.bicep' = {
  scope: rg
  name: 'keyvault'
  params: {
    location: location
    tags: tags
    principalId: principalId
  }
}

module resources 'resources.bicep' = {
  scope: rg
  name: 'resources'
  params: {
    location: location
    tags: tags
    principalId: principalId
    keyvaultName: keyvault.outputs.AZURE_KEY_VAULT_NAME
    openAIKeyName: openAIKeyName
    openAIName: ai.outputs.AZURE_OPENAI_NAME
  }
}

output AZURE_CLIENT_ID string = resources.outputs.MANAGED_IDENTITY_CLIENT_ID
output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN
output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
output AZURE_CONTAINER_REGISTRY string = resources.outputs.AZURE_CONTAINER_REGISTRY
output AZURE_CONTAINER_ENVIRONMENT_NAME string = resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_NAME
output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = resources.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = resources.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output AZURE_KEY_VAULT_ENDPOINT string = keyvault.outputs.AZURE_KEY_VAULT_ENDPOINT
output AZURE_OPENAI_KEY_NAME string = ai.outputs.AZURE_OPENAI_KEY_NAME
output AZURE_OPENAI_ENDPOINT string = ai.outputs.AZURE_OPENAI_ENDPOINT
output AZURE_OPENAI_GPT_NAME string = ai.outputs.AZURE_OPENAI_GPT_NAME
output AZURE_OPENAI_TEXT_EMBEDDING_NAME string = ai.outputs.AZURE_OPENAI_TEXT_EMBEDDING_NAME
output MANAGED_IDENTITY_CLIENT_ID string = resources.outputs.MANAGED_IDENTITY_CLIENT_ID
output SERVICE_BINDING_AZUREQUEUES_ENDPOINT string = resources.outputs.ConnectionStrings__AzureQueues
output SERVICE_BINDING_AZUREBLOBS_ENDPOINT string = resources.outputs.ConnectionStrings__AzureQueues
output ConnectionStrings__AzureQueues string = resources.outputs.ConnectionStrings__AzureQueues
output ConnectionStrings__AzureBlobs string = resources.outputs.ConnectionStrings__AzureQueues
