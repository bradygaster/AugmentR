namespace Backend.Augmentors;

public abstract class AzureQueueBaseAugmentor(SemanticKernelWrapper semanticKernelWrapper,
        ILogger<AzureQueueBaseAugmentor> logger,
        QueueServiceClient queueServiceClient) 
            : BaseAugmentor(semanticKernelWrapper, logger)
{
    protected QueueServiceClient queueServiceClient = queueServiceClient;
}
