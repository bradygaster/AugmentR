namespace Backend.Augmentors;

public class UrlListAugmentor(
    SemanticKernelWrapper semanticKernelWrapper,
    ILogger<UrlListAugmentor> logger,
    BlobServiceClient blobServiceClient,
    QueueServiceClient queueServiceClient,
    LiveUpdateService liveUpdateService)
        : AzureBlobBaseAugmentor(semanticKernelWrapper, logger, blobServiceClient)
{
    public override async Task OnStarted() =>
        await blobServiceClient.GetBlobContainerClient("incoming-urllist").CreateIfNotExistsAsync();

    public override async Task Load()
    {
        var incomingContainerClient = blobServiceClient.GetBlobContainerClient("incoming-urllist");

        var archivedContainerClient = blobServiceClient.GetBlobContainerClient("archive-urllist");
        await archivedContainerClient.CreateIfNotExistsAsync();

        var queueClient = queueServiceClient.GetQueueClient("incoming-urls");
        await queueClient.CreateIfNotExistsAsync();

        var blobs = incomingContainerClient.GetBlobs().Select(x => x.Name).ToList();

        foreach (var blobName in blobs)
        {
            await liveUpdateService.ShowSystemUpdate($"Downloading URL List blob {blobName}.");
            var downloadResult = await incomingContainerClient.GetBlobClient(blobName).DownloadContentAsync();
            var result = downloadResult.Value.Content.ToString();
            await liveUpdateService.ShowSystemUpdate($"Loading URLs from {blobName}.");
            var results = result.Split("\n");
            await liveUpdateService.ShowSystemUpdate($"Loaded {results.Length} URLs from {blobName}.");

            foreach (var url in results)
            {
                await liveUpdateService.ShowSystemUpdate($"Adding URL {url} to queue.");
                await queueClient.SendMessageAsync(JsonSerializer.Serialize(new IncomingUrl { Url = url }));
                await liveUpdateService.ShowSystemUpdate($"Added URL {url} to queue.");
            }

            // archive the blob data
            var archivalBlobClient = archivedContainerClient.GetBlobClient(blobName);
            await archivalBlobClient.UploadAsync(BinaryData.FromString(result), overwrite: true);

            // delete the blob but save it to the archive
            await incomingContainerClient.DeleteBlobAsync(blobName);
        }
    }
}
