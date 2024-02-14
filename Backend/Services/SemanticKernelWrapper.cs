using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Text;
using System.Net;
using System.Text.RegularExpressions;

namespace Backend.Services;

#pragma warning disable SKEXP0003, SKEXP0011, SKEXP0026, SKEXP0055 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public partial class SemanticKernelWrapper(
    IConfiguration configuration,
    ILogger<SemanticKernelWrapper> logger)
{
    private readonly IConfiguration _configuration = configuration;
    private bool _initialized = false;
    private readonly string _collectionName = configuration["COLLECTION_NAME"] ?? "Default";
    private readonly string _gptDeploymentName = configuration["AZURE_OPENAI_GPT_NAME"] ?? string.Empty;
    private readonly string _textEmbeddingDeploymentName = configuration["AZURE_OPENAI_TEXT_EMBEDDING_NAME"] ?? string.Empty;
    private readonly string _openAiEndpoint = configuration["AZURE_OPENAI_ENDPOINT"] ?? string.Empty;
    private readonly string _openAiKey = configuration["AZURE_OPENAI_KEY_NAME"] ?? string.Empty;
    private readonly string _keyVaultEndpoint = configuration["AZURE_KEY_VAULT_ENDPOINT"] ?? string.Empty;
    private ISemanticTextMemory? _semanticTextMemory = null;
    private Kernel? SemanticKernel = null;
    private IChatCompletionService? _chatCompletion = null;

    private bool IsConfigured()
    {
        logger.LogInformation("Checking Semantic Kernel configuration.");

        if (string.IsNullOrEmpty(_gptDeploymentName))
        {
            logger.LogError("The app needs to be configured with the name of a GPT 3.5 Azure OpenAI deployment.");
            return false;
        }
        if (string.IsNullOrEmpty(_textEmbeddingDeploymentName))
        {
            logger.LogError("The app needs to be configured with the name of a Text Embedding Azure OpenAI deployment.");
            return false;
        }
        if (string.IsNullOrEmpty(_openAiEndpoint))
        {
            logger.LogError("The app needs to be configured with an Azure OpenAI endpoint.");
            return false;
        }
        if (string.IsNullOrEmpty(_openAiKey))
        {
            logger.LogError("The app needs to be configured with an Azure OpenAI key secret name.");
            return false;
        }
        if (string.IsNullOrEmpty(_keyVaultEndpoint))
        {
            logger.LogError("The app needs to be configured with an Azure Key Vault endpoint.");
            return false;
        }

        logger.LogInformation("Semantic Kernel configuration check succeeded.");

        return true;
    }

    public async Task InitializeKernel()
    {
        if (IsConfigured())
        {
            try
            {
                logger.LogInformation("Semantic Kernel starting.");

                SecretClient client = new(new Uri(_keyVaultEndpoint), new DefaultAzureCredential());
                var openaikey = await client.GetSecretAsync(_openAiKey);
                var Key = openaikey.Value.Value;

                IKernelBuilder kernelBuilder = Kernel.CreateBuilder()
                    .AddAzureOpenAIChatCompletion(_gptDeploymentName, _openAiEndpoint, Key);
                kernelBuilder.Services.AddLogging(b => b.AddConsole());
                SemanticKernel = kernelBuilder.Build();

                _semanticTextMemory = new MemoryBuilder()
                    .WithLoggerFactory(SemanticKernel.LoggerFactory)
                    .WithAzureOpenAITextEmbeddingGeneration(_textEmbeddingDeploymentName, _openAiEndpoint, Key)
                    .WithMemoryStore(new QdrantMemoryStore(_configuration["QDRANT_ENDPOINT"] ?? "https://qdrant:6333", 1536, SemanticKernel.LoggerFactory))
                    .Build();

                IList<string> collections = await _semanticTextMemory.GetCollectionsAsync();

                logger.LogInformation("Semantic Kernel started.");

                _chatCompletion ??= SemanticKernel.GetRequiredService<IChatCompletionService>();

                logger.LogInformation("Chat completion engine created.");

                _initialized = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during Kernel build-up.");
                _initialized = false;
            }
        }
        else
        {
            _initialized = false;
        }
    }

    public bool IsInitialized() => _initialized;

    public async Task<ChatHistory> Chat(ChatHistory chatHistory)
    {
        if (!IsInitialized())
        {
            await InitializeKernel();
        }

        var stringBuilder = new StringBuilder();
        await foreach (var result in SearchAsync(chatHistory.Last().Content ?? string.Empty))
        {
            stringBuilder.AppendLine(result.Metadata.Text);
        }

        int contextToRemove = -1;

        if (stringBuilder.Length != 0)
        {
            stringBuilder.Insert(0, "Here's some additional information: ");
            contextToRemove = chatHistory.Count;
            chatHistory.AddUserMessage(stringBuilder.ToString());
        }

        stringBuilder.Clear();

        if(_chatCompletion == null)
        {
            throw new ApplicationException("Chat completion engine not initialized.");
        }
        else
        {
            await foreach (var message in _chatCompletion.GetStreamingChatMessageContentsAsync(chatHistory))
            {
                stringBuilder.Append(message.Content);
            }

            chatHistory.AddAssistantMessage(stringBuilder.ToString());
            if (contextToRemove >= 0)
                chatHistory.RemoveAt(contextToRemove);
        }

        return chatHistory;
    }

    public IAsyncEnumerable<MemoryQueryResult> SearchAsync(string content) =>
        _semanticTextMemory?.SearchAsync(_collectionName, content, limit: 5) ?? AsyncEnumerable.Empty<MemoryQueryResult>();

    public async Task<bool> SaveInformation(string content, string id)
    {
        if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(id))
        {
            logger.LogInformation($"Content not saved because either the content or ID was empty.");
        }
        else if (IsInitialized() && _semanticTextMemory != null)
        {
            try
            {
                var f = await _semanticTextMemory.GetAsync(_collectionName, id);
                if (f == null)
                {
                    await _semanticTextMemory.SaveInformationAsync(_collectionName, content, id);
                    logger.LogInformation(
                        "Content saved to collection {CollectionName} with ID/Key {Id}.",
                        _collectionName, id);
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error calling ISemanticTextMemory.SaveInformationAsync with ID/Key {Id}.", id);
            }
        }

        return false;
    }

    public async Task SaveUrl(Uri uri, Func<int, int, Uri, Task>? paragraphCallback = null)
    {
        if (IsInitialized() && _semanticTextMemory != null)
        {
            try
            {
                using HttpClient httpClient = new();

                logger.LogInformation("Getting URI {Uri}.", uri.AbsoluteUri);

                string s = await httpClient.GetStringAsync(uri);

                List<string> paragraphs =
                    TextChunker.SplitPlainTextParagraphs(
                        TextChunker.SplitPlainTextLines(
                            WebUtility.HtmlDecode(NonBreakingSpaceRegex().Replace(s, "")),
                        128),
                    1024);

                for (int i = 0; i < paragraphs.Count; i++)
                {
                    await _semanticTextMemory.SaveInformationAsync(_collectionName, paragraphs[i], $"{uri}{i}");
                    if (paragraphCallback != null)
                    {
                        await paragraphCallback(i, paragraphs.Count, uri);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error calling ISemanticTextMemory.SaveInformationAsync with URL {Uri}.", uri.AbsoluteUri);
            }
        }
    }

    [GeneratedRegex(@"<[^>]+>|&nbsp;")]
    private static partial Regex NonBreakingSpaceRegex();
}
