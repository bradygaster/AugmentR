namespace Backend.Augmentors;

public abstract class AzureBlobBaseAugmentor(SemanticKernelWrapper semanticKernelWrapper,
        ILogger<BaseAugmentor> logger,
        BlobServiceClient blobServiceClient) 
            : BaseAugmentor(semanticKernelWrapper, logger)
{
    protected BlobServiceClient blobServiceClient = blobServiceClient;
}
