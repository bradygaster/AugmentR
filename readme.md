# AugmentR

This is an example of using [Semantic Kernel](https://learn.microsoft.com/semantic-kernel/overview/) in a [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/) application to provide Retrieval Augmented Generation to an existing OpenAI model. The sample includes a way of queueing a list of URLs to be processed by the Semantic Kernel and then using the results to augment the OpenAI model. 

## What is this project? 

Think of AugmentR as a very-simple LLM chatbot you can feed information to in the form of public Internet URLs so *augment* the model with whatever you want to keep it up to date. 

The goal of AugmentR is to show what's possible when you need to add updatable LLMs to your .NET cloud-native apps and to provide an easy-to-understand starter sample using AI from within .NET's Aspire stack. Hopefully, you'll think "What about (insert-your-favorite-document-type-here)?" If that happens, file an issue and submit a pull request to add your own augmentor implementation to read text into the LLM from whatever source you're after.

## What's an 'Augmentor?'

In the case of AugmentR, an Augmentor is a simple .NET class that loads information from some source into an OpenAI model using the Semantic Kernel. For the most part, this whole process has been abstracted away for anyone who wants to extend AugmentR with custom Augmentors. 

AugmentR currently ships with 2 concrete Augmentors:

1. `UrlAugmentor.cs` - Inherits from `AzureQueueBaseAugmentor` to pick messages off of an Azure Storage Queue that contain URLs to be scanned and loaded into the model. 
1. `UrlListAugmentor.cs` - Inherits from `AzureBlobBaseAugmentor` to load text files from Azure Blobs. These text files are presumed to contain one URL per line. This Augmentor opens the file up, then enqueues each of the URLs in the file so the `UrlAugmentor` can load them into the model. 

## Getting Started

The process of getting the sample up and running locally is somewhat simple, provided you have an Azure subscription with OpenAI enablement, and you've installed the Aspire tooling and the Azure Developer CLI. 

> Learn more about these requirements in the [.NET Aspire deployment documentation](https://learn.microsoft.com/dotnet/aspire/deployment/overview). 

Once you've installed the Aspire workload and Visual Studio tools, and the [Azure Developer CLI](https://aka.ms/azd-install), and logged into your Microsoft account from both Visual Studio and the Azure Developer CLI with access to an Azure subscription, do these three steps to get the app set up locally: 

> At this time the repository's setup requires PowerShell and Windows, but I'll add non-Windows support via `bash` very soon.

1. CD into an Empty Directory
1. Initialize the Project

    ```
    azd init -t bradygaster/AugmentR
    ```

    Or, open Visual Studio and start by cloning a repository and paste in that repo URL. 

1. Provision the Azure resources you'll need to run the app: 

    ```
    azd provision
    ```

After about 2 minutes, the Azure Developer CLI will complete the process of provisioning your resources. At this point, it will copy down a series of configuration values and set them as secrets in your app's local configuration using `dotnet user-secrets`. This way, you won't be persisting any configuration in any files, which might accidentally be committed as you experiment with the app. 

F5 the app from within Visual Studio, and it should open up in the Aspire Dashboard. You'll probably see a few errors as the services connect to Azure Queue and Blob Storage at first as things get authenticated. Once the Aspire Dashboard opens, open the `Frontend` project URL in your browser. 

Once open, you can use AugmentR like any other chat-based LLM system, but, when you want to augment the model with additional, updated information, just load in a public URL and let AugmentR do the work for you. 

## Contributions

If you have ideas or want to submit contributions, please file an issue describing what you want or want to contribute. If you add any Augmentors, please update the readme with an introduction to their functionality. 