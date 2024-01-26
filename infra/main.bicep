targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('Names of the resources group. If not provided, a unique name will be generated.')
param resourceGroupName string = ''
param openAiServiceName string = ''
param keyVaultName string = ''
param identityName string = ''
param storageName string = ''
param logAnalyticsName string = ''
param applicationInsightsName string = ''
param containerAppsEnvironmentName string = ''
param containerRegistryName string = ''

@description('String representing the ID of the logged-in user. Get this using ')
param principalId string = ''

@description('Defines if only the dependencies (OpenAI and Storage) are created, or if the container apps are also created.')
param createContainerApps bool = false

@description('Name of the openai key secret in the keyvault')
param openAIKeyName string = 'AZURE-OPEN-AI-KEY'

// load the abbreviations for the resource token
var abbrs = loadJsonContent('./abbreviations.json')

// resource token for naming each resource randomly, reliably
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

// Tags that should be applied to all resources.
var tags = {
  'azd-env-name': environmentName
}

// the openai deployments to create
var openaiDeployment = [
  {
    name: 'gpt${resourceToken}'
    sku: {
      name: 'Standard'
      capacity: 2
    }
    model: {
      format: 'OpenAI'
      name: 'gpt-35-turbo'
      version: '0613'
    }
  }
  {
    name: 'text${resourceToken}'
    sku: {
      name: 'Standard'
      capacity: 5
    }
    model: {
      format: 'OpenAI'
      name: 'text-embedding-ada-002'
      version: '2'
    }
  }
]

// the containing resource group
resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: !empty(resourceGroupName) ? resourceGroupName : '${abbrs.resourcesResourceGroups}${environmentName}'
  location: location
  tags: tags
}

// create the openai resources
module openAi './core/ai/cognitiveservices.bicep' = {
  name: 'openai'
  scope: rg
  params: {
    name: !empty(openAiServiceName) ? openAiServiceName : '${abbrs.cognitiveServicesAccounts}${resourceToken}'
    location: location
    tags: tags
    deployments: openaiDeployment
  }
}

// create the storage resources
module storage './app/storage.bicep' = {
  name: 'app'
  scope: rg
  params: {
    location: location
    keyVaultName: keyvault.outputs.name
    openAIKeyName: openAIKeyName
    identityName: !empty(identityName) ? identityName : '${abbrs.managedIdentityUserAssignedIdentities}${resourceToken}'
    storageName: !empty(storageName) ? storageName : '${abbrs.storageStorageAccounts}${resourceToken}'
    environmentName: environmentName
    openAIName: openAi.outputs.name
    principalId: principalId
  }
}

// create a keyvault to store openai secrets
module keyvault './core/security/keyvault.bicep' = {
  name: 'keyvault'
  scope: rg
  params: {
    name: !empty(keyVaultName) ? keyVaultName : '${abbrs.keyVaultVaults}${resourceToken}'
    location: location
    tags: tags
    principalId: principalId
  }
}

// create the container apps environment if requested
module containers './app/containers.bicep' = if(createContainerApps) {
  name: 'aca'
  scope: rg
  params: {
    location: location
    environmentName: environmentName
    principalId: storage.outputs.principalId
    identityName: storage.outputs.identityName
    keyVaultName: keyvault.outputs.name
    logAnalyticsName: !empty(logAnalyticsName) ? logAnalyticsName : '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    applicationInsightsName: !empty(applicationInsightsName) ? applicationInsightsName : '${abbrs.insightsComponents}${resourceToken}'
    containerAppsEnvironmentName: !empty(containerAppsEnvironmentName) ? containerAppsEnvironmentName : '${abbrs.appManagedEnvironments}${resourceToken}'
    containerRegistryName: !empty(containerRegistryName) ? containerRegistryName : '${abbrs.containerRegistryRegistries}${resourceToken}'
  }
}

// output environment variables
output AZURE_KEY_VAULT_ENDPOINT string = keyvault.outputs.endpoint
output AZURE_CLIENT_ID string = storage.outputs.AZURE_CLIENT_ID
output AZURE_OPENAI_KEY_NAME string = openAIKeyName
output AZURE_OPENAI_ENDPOINT string = openAi.outputs.endpoint
output AZURE_OPENAI_GPT_NAME string = storage.outputs.AI_GPT_DEPLOYMENT_NAME
output AZURE_OPENAI_TEXT_EMBEDDING_NAME string = storage.outputs.AI_TEXT_DEPLOYMENT_NAME
output ConnectionStrings__AzureQueues string = storage.outputs.AZURE_QUEUE_ENDPOINT
output ConnectionStrings__AzureBlobs string = storage.outputs.AZURE_BLOB_ENDPOINT
output AZURE_CONTAINER_REGISTRY string = ((createContainerApps) ? containers.outputs.AZURE_CONTAINER_REGISTRY : '')
