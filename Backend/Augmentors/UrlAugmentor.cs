namespace Backend.Augmentors;

public class UrlAugmentor(SemanticKernelWrapper semanticKernelWrapper,
    ILogger<UrlAugmentor> logger,
    QueueServiceClient queueServiceClient,
    HistoryApiClient historyApiClient,
    LiveUpdateService liveUpdateService)
        : AzureQueueBaseAugmentor(semanticKernelWrapper, logger, queueServiceClient)
{
    private readonly HistoryApiClient historyApiClient = historyApiClient;
    private readonly LiveUpdateService liveUpdateService = liveUpdateService;

    public override async Task Load()
    {
        var incomingQueueClient = queueServiceClient.GetQueueClient("incoming-urls");
        await incomingQueueClient.CreateIfNotExistsAsync();

        QueueMessage[] messages = await incomingQueueClient.ReceiveMessagesAsync(maxMessages: 8);

        foreach (var message in messages)
        {
            if (message.DequeueCount <= 2)
            {
                var incomingUrl = JsonSerializer.Deserialize<IncomingUrl>(message.MessageText);
                if (incomingUrl != null)
                {
                    // try to create the uri
                    Uri? parsedUri = null;

                    // if the uri is legit, save it to the model
                    if (Uri.TryCreate(incomingUrl.Url, UriKind.Absolute, out parsedUri))
                    {
                        await historyApiClient.SaveHistoryItem(CreateHistoricalItem("None", incomingUrl.Url, $"Augmenting model with Url: {incomingUrl.Url}"));

                        await semanticKernelWrapper.SaveUrl(parsedUri, async (index, total, activeUri) =>
                        {
                            await historyApiClient.SaveHistoryItem(
                                CreateHistoricalItem($"{incomingUrl.Url}{index + 1}", incomingUrl.Url, $"Augmenting model with Url: {activeUri.AbsoluteUri}. On paragraph {index + 1} of {total}")
                                );
                            await liveUpdateService.ShowSystemUpdate($"Augmenting model with Url: {activeUri.AbsoluteUri}. On paragraph {index + 1} of {total}");
                        });

                        await historyApiClient.SaveHistoryItem(CreateHistoricalItem("None", incomingUrl.Url, $"Augmented model with Url: {incomingUrl.Url}"));
                        await liveUpdateService.ShowSystemUpdate($"Augmented model with Url: {incomingUrl.Url}");
                    }
                }
            }

            // delete the message
            await incomingQueueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
        }
    }

    static HistoricalItem CreateHistoricalItem(string contentId, string url, string description)
        => new HistoricalItem
        {
            ContentId = contentId,
            SourceUrl = url,
            SourceType = "Url",
            Description = description,
            Timestamp = DateTime.UtcNow
        };
}