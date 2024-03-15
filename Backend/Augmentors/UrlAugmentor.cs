using Azure.AI.OpenAI;

namespace Backend.Augmentors;

public class UrlAugmentor(SemanticKernelWrapper semanticKernelWrapper,
    ILogger<UrlAugmentor> logger,
    QueueServiceClient queueServiceClient,
    LiveUpdateService liveUpdateService,
    OpenAIClient openAiClient)
        : AzureQueueBaseAugmentor(semanticKernelWrapper, logger, queueServiceClient)
{
    private readonly LiveUpdateService liveUpdateService = liveUpdateService;
    private readonly OpenAIClient openAiClient = openAiClient;

    public override async Task OnStarted() =>
        await queueServiceClient.GetQueueClient("incoming-urls").CreateIfNotExistsAsync();

    public override async Task Load()
    {
        var incomingQueueClient = queueServiceClient.GetQueueClient("incoming-urls");

        QueueMessage[] messages = await incomingQueueClient.ReceiveMessagesAsync(maxMessages: 8);

        foreach (var message in messages)
        {
            if (message.DequeueCount <= 2)
            {
                var incomingUrl = JsonSerializer.Deserialize<IncomingUrl>(message.MessageText);
                if (incomingUrl != null)
                {
                    // try to create the uri

                    // if the uri is legit, save it to the model
                    if (Uri.TryCreate(incomingUrl.Url, UriKind.Absolute, out Uri? parsedUri))
                    {
                        await semanticKernelWrapper.SaveUrl(parsedUri, async (index, total, activeUri) =>
                        {
                            await liveUpdateService.ShowSystemUpdate($"Augmenting model with Url: {activeUri.AbsoluteUri}. On paragraph {index + 1} of {total}");
                        });

                        await liveUpdateService.ShowSystemUpdate($"Augmented model with Url: {incomingUrl.Url}");
                    }
                }
            }

            // delete the message
            await incomingQueueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
        }
    }

    static HistoricalItem CreateHistoricalItem(string contentId, string url, string description)
        => new()
        {
            ContentId = contentId,
            SourceUrl = url,
            SourceType = "Url",
            Description = description,
            Timestamp = DateTime.UtcNow
        };
}