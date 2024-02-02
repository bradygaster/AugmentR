# AugmentR

This is an example of using [Semantic Kernel](https://learn.microsoft.com/semantic-kernel/overview/) in a [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/) application to provide Retrieval Augmented Generation to an existing OpenAI model. The sample includes a way of queueing a list of URLs to be processed by the Semantic Kernel and then using the results to augment the OpenAI model. 

> Note: You're looking at a branch that represents the code in a state where you build up the automated provision and deployment capability. If you want a branch of the app that is deployable right now, switch to the `main` branch. This branch of the app is for folks who want to understand the delta between the automagic integration of AZD and Aspire and needing to customize the deployment for [insert reason here]. Most large apps will need some sort of deployment customization, so we chose a relatively complex scenario - an augmentable OpenAI chatbot - to demonstrate how you can let the automagic part get you started but then customize the build per your needs. 

## Getting Started

This is the initial structure of the app, with none of the AZD configuration or generated Infrastructure-as-Code (IAC) files you'll need to deploy AugmentR to Azure. The goal of this branch of the repo is to walk you through these scenarios: 

1. Initializing your Azure Developer CLI environment from the Aspire app solution
1. Customizing the IAC generated by AZD
1. Configuring the local environment to use live Azure resources during run/debug experiences
1. Deploying the app to Azure

Let's get into it!

---

### Initializing your AZD environment

First up, you'll use `azd init` to initialize the local environment for use with AZD. To do this:

1. Run this command in the root directory of the repository:

    ```
    azd init
    ```

1. AZD will scan the solution, find the Aspire App Host project, and figure out most of what to do. You'll need to specify that the `frontend` project should be the one project allowing internet traffic.

1. Finally, you'll give your AZD environment a name. Something like `augmentr02012024003` should be fine. The environment isn't associated with your `ASPNETCORE_ENVIRONMENT` or anything like that. Think of an AZD environment as one independent deployment of all the Azure resources in your app. 

1. One of the files produced when you run `azd.init` is the `azure.yaml` file: 

    ```yaml
    # yaml-language-server: $schema=https://raw.gith`ubusercontent.com/Azure/azure-dev/main/schemas/v1.0/azure.yaml.json

    name: AugmentR
    services:  
      app:
        language: dotnet
        project: .\AppHost\AppHost.csproj
        host: containerapp
    ```

    You'll also see files in a `.azure` folder, separated into directories. Each directory represents an individual AZD environment, and each environment folder contains an independent `.env` file into which AZD will generate environment variables. 

    ```bash
    AZURE_ENV_NAME="augmentr02012024003"
    ```
    
    Finally, the `config.json` file in the `.azure` folder will specify the current environment. 

    ```json
    { 
        "version" : 1, 
        "defaultEnvironment" : "augmentr02012024003" 
    }
    ```

1. The next step will synthesize all of the IAC needed for the app and drop it to disk. 

    ```
    azd infra synth
    ```

`azd infra synth` will tell AZD to parse the .NET Aspire app's manifest and generate a series of manifest files in my source code tree for use when  want to customize the deployment of the app. 

---

### Customizing the IAC generated by AZD

At this point, the deployment is *almost* ready but there are a few customizations you'll need to make before you can debug the code locally. 

* Each of the .NET projects in the Aspire solution only needs to have one instance running per Container Apps revision, and the default setup allows for scaling from 1-10. You'll customize the manifest 

* The IAC code also needs to be augmented with custom Bicep code to provision an Azure OpenAI resources and two specific deployments for GPT and text embedding capabilities. You'll need to add that code before provisioning or you won't get an Azure OpenAI instance.

* The IAC code generated by AZD includes the provisioning of a managed identity used by the app to authenticate itself to Azure resources. It lacks code that enables your local user account the same priveleges. As such, you'll need to edit the IAC code generated by AZD to create roles for your account, as well as the app's account. 

#### Customizing scaling rules 

Inside of each of the C# project folders, a new `manifests` folder has been generated by `azd infra synth`. 

1. Open `Backend\manifests\containerApp.tmpl.yaml`, as an example, you'll see YAML code that you can use to override any of the Bicep nodes in the generated IAC.

    ```yaml
    location: {{ .Env.AZURE_LOCATION }}
    identity:
    type: UserAssigned
    userAssignedIdentities:
        ? "{{ .Env.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID }}"
        : {}
    properties:
    environmentId: {{ .Env.AZURE_CONTAINER_APPS_ENVIRONMENT_ID }}
    configuration:
        activeRevisionsMode: single
        ingress:
          external: false
          targetPort: 8080
          transport: http
          allowInsecure: true
        registries:
        - server: {{ .Env.AZURE_CONTAINER_REGISTRY_ENDPOINT }}
        identity: {{ .Env.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID }}
    template:
        containers:
        - image: {{ .Image }}
        name: backend
        env:
        - name: AZURE_CLIENT_ID
          value: {{ .Env.MANAGED_IDENTITY_CLIENT_ID }}
        - name: ConnectionStrings__AzureBlobs
          value: {{ .Env.SERVICE_BINDING_AZUREBLOBS_ENDPOINT }}
        - name: ConnectionStrings__AzureQueues
          value: {{ .Env.SERVICE_BINDING_AZUREQUEUES_ENDPOINT }}
        - name: ConnectionStrings__pubsub
          value: {{ connectionString "pubsub" }}
        - name: OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES
          value: "true"
        - name: OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES
          value: "true"
        - name: QDRANT_ENDPOINT
          value: http://qdrant.internal.{{ .Env.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN }}
        - name: services__historyservice__0
          value: http://historyservice.internal.{{ .Env.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN }}
        - name: services__historyservice__1
          value: https://historyservice.internal.{{ .Env.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN }}
        scale:
            minReplicas: 1
    tags:
    azd-service-name: backend
    aspire-resource-name: backend
    ```

1. Take note of the `scale` node late in the document. Edit this node so that it no longer has just the `minReplices`, but also includes a `maxReplicas` node also set to 1.

    ```yaml
    scale:
        minReplicas: 1
        maxReplicas: 1
    ```

    The outcome of this is that the `backend` node of the app in the AugmentR app will run as a single-instance node. 

    You'll repeat this process for each of the project manifest files in the AugmentR solution tree: 

    * `Backend\manifests\containerApp.tmpl.yaml`
    * `Frontend\manifests\containerApp.tmpl.yaml`
    * `HistoryDb\manifests\containerApp.tmpl.yaml`
    * `HistoryService\manifests\containerApp.tmpl.yaml`

    > In cases where you'd want to reap the benefits of Azure Container Apps' KEDA-driven scaling, you'd want to add support in your app code for distributed-safe Data Protection, using Blobs and/or Key Vault, or a relational database. 

#### Using Bicep Modules

In the next phase of the customization, you'll re-use some existing Bicep files - or **modules** - from the Azure Core Bicep samples. These files will make it simpler for you to create Roles, Permissions, and AI-related resources AugmentR will need. This is helpful when you've got a lot of Bicep assets you'd like to bring to the table, but want to reap some of the benefits of the new AZD + Aspire integration, too. 

1. Create two new folders under the `infra` folder - `infra/core/ai` and `infra/core/security`. 

1. Create a new file in `infra/core/ai` named `cognitiveservices.bicep`. Copy this code into that file: 

    ```bicep
    metadata description = 'Creates an Azure Cognitive Services instance.'
    param name string
    param location string = resourceGroup().location
    param tags object = {}
    param deployments array = []
    param kind string = 'OpenAI'
    
    @description('The custom subdomain name used to access the API. Defaults to the value of the name parameter.')
    param customSubDomainName string = name

    @allowed([ 'Enabled', 'Disabled' ])
        param publicNetworkAccess string = 'Enabled'
        param sku object = {
        name: 'S0'
    }

    param allowedIpRules array = []
    param networkAcls object = empty(allowedIpRules) ? {
        defaultAction: 'Allow'
    } : {
        ipRules: allowedIpRules
        defaultAction: 'Deny'
    }

    resource account 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
        name: name
        location: location
        tags: tags
        kind: kind
        properties: {
            customSubDomainName: customSubDomainName
            publicNetworkAccess: publicNetworkAccess
            networkAcls: networkAcls
        }
        sku: sku
    }

    @batchSize(1)
        resource deployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = [for deployment in deployments: {
        parent: account
        name: deployment.name
        properties: {
            model: deployment.model
            raiPolicyName: contains(deployment, 'raiPolicyName') ? deployment.raiPolicyName : null
        }
        sku: contains(deployment, 'sku') ? deployment.sku : {
            name: 'Standard'
            capacity: 20
        }
    }]

    output endpoint string = account.properties.endpoint
    output id string = account.id
    output name string = account.name
    ```

1. Create a new file in `infra/core/security` named `keyvault-access.bicep`. Copy this code into that file: 

    ```bicep
    metadata description = 'Assigns an Azure Key Vault access policy.'
    param name string = 'add'

    param keyVaultName string
    param permissions object = { secrets: [ 'get', 'list' ] }
    param principalId string

    resource keyVaultAccessPolicies 'Microsoft.KeyVault/vaults/accessPolicies@2022-07-01' = {
        parent: keyVault
        name: name
        properties: {
            accessPolicies: [ {
                objectId: principalId
                tenantId: subscription().tenantId
                permissions: permissions
            } ]
        }
    }

    resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
        name: keyVaultName
    }
    ```

1. Create a new file in `infra/core/security` named `keyvault-secret.bicep`. Copy this code into that file: 

    ```bicep
    metadata description = 'Creates or updates a secret in an Azure Key Vault.'

    param name string
    param tags object = {}
    param keyVaultName string
    param contentType string = 'string'
    param enabled bool = true
    param exp int = 0
    param nbf int = 0
    
    @description('The value of the secret. Provide only derived values like blob storage access, but do not hard code any secrets in your templates')
    @secure()
    param secretValue string

    resource keyVaultSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
        name: name
        tags: tags
        parent: keyVault
        properties: {
            attributes: {
            enabled: enabled
            exp: exp
            nbf: nbf
            }
            contentType: contentType
            value: secretValue
        }
    }

    resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
        name: keyVaultName
    }
    ```

1. Create a new file in `infra/core/security` named `keyvault.bicep`. Copy this code into that file: 

    ```bicep
    metadata description = 'Creates an Azure Key Vault.'

    param name string
    param location string = resourceGroup().location
    param tags object = {}
    param principalId string = ''

    resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = {
        name: name
        location: location
        tags: tags
        properties: {
            tenantId: subscription().tenantId
            sku: { family: 'A', name: 'standard' }
            accessPolicies: !empty(principalId) ? [
            {
                objectId: principalId
                permissions: { secrets: [ 'get', 'list' ] }
                tenantId: subscription().tenantId
            }
            ] : []
        }
    }

    output endpoint string = keyVault.properties.vaultUri
    output name string = keyVault.name
    ```

1. Create a new file in `infra/core/security` named `role.bicep`. Copy this code into that file: 

    ```bicep
    metadata description = 'Creates a role assignment for a service principal.'

    param principalId string

    @allowed([
        'Device'
        'ForeignGroup'
        'Group'
        'ServicePrincipal'
        'User'
    ])

    param principalType string = 'ServicePrincipal'
    param roleDefinitionId string

    resource role 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
        name: guid(subscription().id, resourceGroup().id, principalId, roleDefinitionId)
        properties: {
            principalId: principalId
            principalType: principalType
            roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', roleDefinitionId)
        }
    }
    ```

1. Create a new file in the `infra` folder named `ai.bicep` and paste in this code.


    ```bicep
    @description('The location used for all deployed resources')
    param location string = resourceGroup().location

    @description('Tags that will be applied to all resources')
    param tags object = {}

    @description('Name of the openai key secret in the keyvault')
    param openAIKeyName string = 'AZURE-OPEN-AI-KEY'

    var resourceToken = uniqueString(resourceGroup().id)

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

    // create the openai resources
    module openAi './core/ai/cognitiveservices.bicep' = {
        name: 'openai'
        scope: resourceGroup()
        params: {
            name: 'ai${resourceToken}'
            location: location
            tags: tags
            deployments: openaiDeployment
        }
    }

    output AZURE_OPENAI_ENDPOINT string = openAi.outputs.endpoint
    output AZURE_OPENAI_GPT_NAME string = 'gpt${resourceToken}'
    output AZURE_OPENAI_KEY_NAME string = openAIKeyName
    output AZURE_OPENAI_NAME string = 'ai${resourceToken}'
    output AZURE_OPENAI_TEXT_EMBEDDING_NAME string = 'text${resourceToken}'
    ```

1. Create a new file in the `infra` folder named `keyvault.bicep` and paste in this code.

    ```bicep
    @description('The location used for all deployed resources')
    param location string = resourceGroup().location

    @description('Tags that will be applied to all resources')
    param tags object = {}

    @description('String representing the ID of the logged-in user ')
    param principalId string = ''

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

    output AZURE_KEY_VAULT_ENDPOINT string = keyvault.outputs.endpoint
    output AZURE_KEY_VAULT_NAME string = keyvault.name
    ```

1. Copy this code to create a `principalId` node using the `AZURE_PRINCIPAL_ID` environment variable on your machine, representing your logged-in account. 

    ```json
    "principalId": {
        "value": "${AZURE_PRINCIPAL_ID}"
      }
    ```

1. Open `infra/main.parameters.json` file, which should look like:

    ```json
    {
        "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
        "contentVersion": "1.0.0.0",
        "parameters": {
            "environmentName": {
                "value": "${AZURE_ENV_NAME}"
            },
            "location": {
                "value": "${AZURE_LOCATION}"
            }
        }
    }

1. Paste the `principalId` node in you copied earlier as a new node, resulting in this structure:

    ```json
    {
        "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
        "contentVersion": "1.0.0.0",
        "parameters": {
            "environmentName": {
                "value": "${AZURE_ENV_NAME}"
            },
            "location": {
                "value": "${AZURE_LOCATION}"
            },
            "principalId": {
                "value": "${AZURE_PRINCIPAL_ID}"
            }
        }
    }
    ```

1. Add the `principalId` and `openAIKeyName` parameters before the `tags` parameter in `infra/main.bicep`.

    ```bicep
    @description('String representing the ID of the logged-in user')
    param principalId string = ''

    @description('Name of the openai key secret in the keyvault')
    param openAIKeyName string = 'AZURE-OPEN-AI-KEY'
    ```

1. Between the `tags` and `resourceToken` parameters in `resources.bicep`, add these parameters: 

    ```bicep
    @description('String representing the ID of the logged-in user')
    param principalId string = ''

    @description('Name of the key vault used by the app')
    param keyvaultName string = ''

    @description('Name of the openai key secret in the keyvault')
    param openAIKeyName string = 'AZURE-OPEN-AI-KEY'

    @description('Name of the openai key secret in the keyvault')
    param openAIName string
    ```

1. Remove this section - the `storageBlobsRoleAssignment` and `storageQueuesRoleAssignment` nodes - from the `resources.bicep` file altogether. 

    ```bicep
    resource storageBlobsRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
        name: guid(storage.id, managedIdentity.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'))
        scope: storage
        properties: {
            principalId: managedIdentity.properties.principalId
            principalType: 'ServicePrincipal'
            roleDefinitionId:  subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
        }
    }
    resource storageQueuesRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
        name: guid(storage.id, managedIdentity.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88'))
        scope: storage
        properties: {
            principalId: managedIdentity.properties.principalId
            principalType: 'ServicePrincipal'
            roleDefinitionId:  subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')
        }
    }
    ```

With these changes made, you're ready to make the final change - setting roles and permissions across all the resources so your deployed app *and* you, logged in on your development machine, can use these cloud resources. 

#### Setting roles and permissions

In this last step, you'll add code to `resources.bicep` to set roles and permissions for your app and your account.

1. Copy the following Bicep code for all of the permissions needed for both the app running in Azure, and the app running locally on your computer signed in as you.

    ```bicep
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
    ```

1. Paste the code you just copied into the `resources.bicep` file between the `storage` resource and the `MANAGED_IDENTITY_CLIENT_ID` output parameter.

    ```bicep
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

    // paste the code you just copied here

    output MANAGED_IDENTITY_CLIENT_ID string = managedIdentity.properties.clientId
    ```

### Finalizing the provisioning

In these last few steps, you'll add code to `main.bicep` that uses all the work you've done up to this point. You'll provision all the dependencies, hand some environment variables back that you'll save into `dotnet user-secrets`, and finally, run the code locally and deploy it to Azure. 

1. In `main.bicep`, add the `ai` and `keyvault` modules between the `rg` resources representing the resource group in which the app is being provisioned and the `resources` module. 

    ```bicep
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
    ```

    Next, you'll tweak the `params` property of the `resources` node to reflect the updated parameters `resources` needs: 

    ```bicep
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
    ```

1. The `ai` module will output some environment variables you'll need to pump back through to the `backend` project's configuration. Add those to the end of `main.bicep` after the `SERVICE_BINDING_AZUREQUEUES_ENDPOINT` output parameter. 

    ```bicep
    output AZURE_KEY_VAULT_ENDPOINT string = keyvault.outputs.AZURE_KEY_VAULT_ENDPOINT
    output AZURE_OPENAI_KEY_NAME string = ai.outputs.AZURE_OPENAI_KEY_NAME
    output AZURE_OPENAI_ENDPOINT string = ai.outputs.AZURE_OPENAI_ENDPOINT
    output AZURE_OPENAI_GPT_NAME string = ai.outputs.AZURE_OPENAI_GPT_NAME
    output AZURE_OPENAI_TEXT_EMBEDDING_NAME string = ai.outputs.AZURE_OPENAI_TEXT_EMBEDDING_NAME
    ```

1. Also, account for these Azure OpenAI configuration values in the `backend\manifests\containerApp.tmpl.yaml` file to make sure those values are pumped through to the container app when it is provisioned and deployed. Add these nodes to the end of the `env` array.

    ```yaml
    - name: AZURE_OPENAI_GPT_NAME
      value: {{ .Env.AZURE_OPENAI_GPT_NAME }}
    - name: AZURE_OPENAI_TEXT_EMBEDDING_NAME
      value: {{ .Env.AZURE_OPENAI_TEXT_EMBEDDING_NAME }}
    - name: AZURE_OPENAI_ENDPOINT
      value: {{ .Env.AZURE_OPENAI_ENDPOINT }}
    - name: AZURE_OPENAI_KEY_NAME
      value: {{ .Env.AZURE_OPENAI_KEY_NAME }}
    - name: AZURE_KEY_VAULT_ENDPOINT
      value: {{ .Env.AZURE_KEY_VAULT_ENDPOINT }}
    ```

---

With these changes in place, you're ready to provision the app in such a manner that it'll be ready for you to dev against and, eventually, deploy into. Before you do, though, there's one last step to make sure that, when you do, the variety of environment variables that will be emitted from the AZD provisioning process are copied into your `dotnet user-secrets` settings so your code can run locally but use the live Azure resources. 

--- 

### Configuring the local environment to use live Azure resources

You're going to need to debug the app **locally** on your development machine, but, it'll be using **live** resources running in the cloud. As such, you'll need to copy the environment variables for all of the various Azure resources created by AZD back into your `dotnet user-secrets` collection. To do this, 

1. Create a new file in the root of the repo named `postprovision.ps1`. Paste this code into the file: 


    ```powershell
    function Set-DotnetUserSecrets {
        param ($path, $lines)
        Push-Location
        cd $path
        dotnet user-secrets clear
        foreach ($line in $lines) {
            $name, $value = $line -split '='
            $value = $value -replace '"', ''
            $name = $name -replace '__', ':'
            if ($value -ne '') {
                dotnet user-secrets set $name $value | Out-Null
            }
        }
        Pop-Location
    }

    $lines = (azd env get-values) -split "`n"
    Set-DotnetUserSecrets -path ".\Backend" -lines $lines
    Set-DotnetUserSecrets -path ".\Frontend" -lines $lines
    ```

1. Copy this YAML code to your clipboard. In a moment you'll paste it into `azure.yaml` as a post-provisioning hook script. This means your PowerShell code will run after the resources have been provisioned, pumping all of the environment variables your code will need to run locally into your `dotnet user-secrets` collection. 

1. Open the `azure.yaml` file, which should look like this: 

    ```yaml
    # yaml-language-server: $schema=https://raw.githubusercontent.com/Azure/azure-dev/main/schemas/v1.0/azure.yaml.json

    name: 02-end
    services:  
    app:
      language: dotnet
      project: .\AppHost\AppHost.csproj
       host: containerapp
    ```

1. Paste the code you just copied at the end of the file, so you'll copy the settings into your secrets when provision succeeds: 

    ```yaml
    # yaml-language-server: $schema=https://raw.githubusercontent.com/Azure/azure-dev/main/schemas/v1.0/azure.yaml.json

    name: AugmentR
    services:  
    app:
      language: dotnet
      project: .\AppHost\AppHost.csproj
      host: containerapp

    hooks:
    postprovision:
      windows:
        shell: pwsh
          run: ./postprovision.ps1
          interactive: true
          continueOnError: true
    ```

Now, you're ready to run `azd provision` to create the resources and configure your local development environment to run the code and customize it should you have any ideas you'd like to contribute. 

---

## Provision the resources

Next, open your terminal and, at the root of the repository in the same directory as the `.sln` file:

1. Provision the resources:

    ```
    azd provision
    ```

1. Once the resources are created you can use `dotnet run` or Visual Studio's F5 debug/run feature to test the app out end-to-end. 

Once you're sure everything works, deployment is super-simple!

## Deploy the app

Deployment is easy since you already have an AZD environment.

1. Run `deploy` to deploy the app. 

    ```
    azd deploy
    ```

    Wait for a few minutes whilst AZD deploys the app. Once it completes, you'll see a link to the `frontend` app in the terminal. Ctrl-click the link and you should have your own Aspire and Azure OpenAI-powered chatbot. 

## Contributions

If you have ideas or want to submit contributions, please file an issue describing what you want or want to contribute. If you add any Augmentors, please update the readme with an introduction to their functionality. 