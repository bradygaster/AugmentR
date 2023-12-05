targetScope = 'subscription'

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

@description('Defines if only the dependencies (OpenAI and Storage) are created, or if the container apps are also created.')
param createContainerApps bool = false

// resource token for naming each resource randomly, reliably
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

// Tags that should be applied to all resources.
var tags = {
  'azd-env-name': environmentName
}

// the containing resource group
resource group 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

// create the openai and storage resources
module dependencies 'dependencies.bicep' = {
  name: 'app${resourceToken}'
  scope: group
  params: {
    location: location
    environmentName: environmentName
    myUserId: myUserId
  }
}

// output environment variables
output AZURE_CLIENT_ID string = dependencies.outputs.AZURE_CLIENT_ID
output AZUREOPENAI_ENDPOINT string = dependencies.outputs.AZUREOPENAI_ENDPOINT
output AZUREOPENAI_API_KEY string = dependencies.outputs.AZUREOPENAI_KEY
output AZUREOPENAI_GPT_NAME string = dependencies.outputs.AI_GPT_DEPLOYMENT_NAME
output AZUREOPENAI_TEXT_EMBEDDING_NAME string = dependencies.outputs.AI_TEXT_DEPLOYMENT_NAME
output ConnectionStrings__AzureQueues string = dependencies.outputs.AZURE_QUEUE_ENDPOINT
output ConnectionStrings__AzureBlobs string = dependencies.outputs.AZURE_BLOB_ENDPOINT

// create the container apps environment if requested
module containers 'containers.bicep' = if(createContainerApps) {
  name: 'app${resourceToken}'
  scope: group
  params: {
    location: location
    environmentName: environmentName
  }
}
output AZURE_CONTAINER_REGISTRY string = ((createContainerApps) ? containers.outputs.AZURE_CONTAINER_REGISTRY : '')
