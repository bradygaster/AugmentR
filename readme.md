# AugmentR

This is an example of using [Semantic Kernel](https://learn.microsoft.com/semantic-kernel/overview/) in a [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/) application to provide Retrieval Augmented Generation to an existing OpenAI model. The sample includes a way of queueing a list of URLs to be processed by the Semantic Kernel and then using the results to augment the OpenAI model. 

> Note: You're looking at a branch that represents an incomplete state. If you want a branch of the app that is deployable right now, switch to the `main` branch. 

## What is this project? 

Think of AugmentR as a very-simple LLM chatbot you can feed information to in the form of public Internet URLs so *augment* the model with whatever you want to keep it up to date. 

The goal of AugmentR is to show what's possible when you need to add updatable LLMs to your .NET cloud-native apps and to provide an easy-to-understand starter sample using AI from within .NET's Aspire stack. Hopefully, you'll think "What about (insert-your-favorite-document-type-here)?" If that happens, file an issue and submit a pull request to add your own augmentor implementation to read text into the LLM from whatever source you're after.

## Getting Started

This is the initial structure of the app, with none of the AZD configuration or generated Infrastructure-as-Code (IAC) files you'll need to deploy AugmentR to Azure. The goal of this branch of the repo is to walk you through these scenarios: 

1. Initializing your Azure Developer CLI environment from the Aspire app solution
1. Provisioning the live Azure resources you'll need to run/debug the app
1. Configuring the local environment to use live Azure resources during run/debug experiences
1. Deploying the app to Azure

## Contributions

If you have ideas or want to submit contributions, please file an issue describing what you want or want to contribute. If you add any Augmentors, please update the readme with an introduction to their functionality. 