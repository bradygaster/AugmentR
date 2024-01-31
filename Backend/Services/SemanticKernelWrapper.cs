using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Text;
using System.Net;
using System.Text.RegularExpressions;

using Azure.Identity;  
using Azure.Security.KeyVault.Secrets;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Backend.Services;

#pragma warning disable SKEXP0003, SKEXP0011, SKEXP0026, SKEXP0055 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public class SemanticKernelWrapper(IConfiguration configuration,
        ILogger<SemanticKernelWrapper> logger)
{
    private ILogger<SemanticKernelWrapper> _logger = logger;
    private IConfiguration _configuration = configuration;
    private bool _initialized = false;
    private string _collectionName = configuration["COLLECTION_NAME"] ?? "Default";
    private string _gptDeploymentName = configuration["AZURE_OPENAI_GPT_NAME"] ?? string.Empty;
    private string _textEmbeddingDeploymentName = configuration["AZURE_OPENAI_TEXT_EMBEDDING_NAME"] ?? string.Empty;
    private string _openAiEndpoint = configuration["AZURE_OPENAI_ENDPOINT"] ?? string.Empty;
    private string _openAiKey = configuration["AZURE_OPENAI_KEY_NAME"] ?? string.Empty;
    private string _keyVaultEndpoint = configuration["AZURE_KEY_VAULT_ENDPOINT"] ?? string.Empty;
    private ISemanticTextMemory? _semanticTextMemory = null;
    private Kernel? SemanticKernel = null;
    private IChatCompletionService? _chatCompletion = null;

    private bool IsConfigured()
    {
        _logger.LogInformation("Checking Semantic Kernel configuration.");

        if (string.IsNullOrEmpty(_gptDeploymentName))
        {
            _logger.LogError("The app needs to be configured with the name of a GPT 3.5 Azure OpenAI deployment.");
            return false;
        }
        if (string.IsNullOrEmpty(_textEmbeddingDeploymentName))
        {
            _logger.LogError("The app needs to be configured with the name of a Text Embedding Azure OpenAI deployment.");
            return false;
        }
        if (string.IsNullOrEmpty(_openAiEndpoint))
        {
            _logger.LogError("The app needs to be configured with an Azure OpenAI endpoint.");
            return false;
        }
        if (string.IsNullOrEmpty(_openAiKey))
        {
            _logger.LogError("The app needs to be configured with an Azure OpenAI key secret name.");
            return false;
        }
        if (string.IsNullOrEmpty(_keyVaultEndpoint))
        {
            _logger.LogError("The app needs to be configured with an Azure keyvault endpoint.");
            return false;
        }

        _logger.LogInformation("Semantic Kernel configuration check succeeded.");

        return true;
    }

    public async Task InitializeKernel()
    {
        if (IsConfigured())
        {
            try
            {
                _logger.LogInformation("Semantic Kernel starting.");

                SecretClient client = new SecretClient(new Uri(_keyVaultEndpoint), new DefaultAzureCredential());
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

                _logger.LogInformation("Semantic Kernel started.");

                _chatCompletion = _chatCompletion ??
                    SemanticKernel.GetRequiredService<IChatCompletionService>();

                _logger.LogInformation("Chat completion engine created.");

                _initialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Kernel build-up.");
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
            _logger.LogInformation($"Content not saved because either the content or ID was empty.");
        }
        else if (IsInitialized() && _semanticTextMemory != null)
        {
            try
            {
                var f = await _semanticTextMemory.GetAsync(_collectionName, id);
                if (f == null)
                {
                    await _semanticTextMemory.SaveInformationAsync(_collectionName, content, id);
                    _logger.LogInformation($"Content saved to collection {_collectionName} with ID/Key {id}.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calling ISemanticTextMemory.SaveInformationAsync with ID/Key {id}.");
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
                using (HttpClient httpClient = new())
                {
                    _logger.LogInformation($"Getting URI {uri.AbsoluteUri}.");

                    string s = await httpClient.GetStringAsync(uri);

                    List<string> paragraphs =
                        TextChunker.SplitPlainTextParagraphs(
                            TextChunker.SplitPlainTextLines(
                                WebUtility.HtmlDecode(Regex.Replace(s, @"<[^>]+>|&nbsp;", "")),
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calling ISemanticTextMemory.SaveInformationAsync with URL {uri.AbsoluteUri}.");
            }
        }
    }
}
