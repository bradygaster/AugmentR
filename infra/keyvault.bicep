@description('The location used for all deployed resources')
param location string = resourceGroup().location

@description('Tags that will be applied to all resources')
param tags object = {}

@description('String representing the ID of the logged-in user ')
param principalId string = ''

@description('String representing the name of the KeyVault Key representing the Azure OpenAI API Key secret')
param openAIKeyName string = ''

@description('String representing the name of the Azure OpenAI resource')
param openAIName string = ''

var resourceToken = uniqueString(resourceGroup().id)

// create a keyvault to store openai secrets
module keyvault 'core/security/keyvault.bicep' = {
    name: 'kv${resourceToken}'
    scope: resourceGroup()
    params: {
        name: 'kv${resourceToken}'
        location: location
        tags: tags
        principalId: principalId
    }
}

// create secret to store openai api key
module openAIKey 'core/security/keyvault-secret.bicep' = {
    name: 'openai-key'
    params: {
        name: openAIKeyName
        keyVaultName: keyvault.name
        secretValue: listKeys(resourceId(subscription().subscriptionId, resourceGroup().name, 'Microsoft.CognitiveServices/accounts', openAIName), '2023-05-01').key1
    }
}

output AZURE_KEY_VAULT_ENDPOINT string = keyvault.outputs.endpoint
output AZURE_KEY_VAULT_NAME string = keyvault.name
