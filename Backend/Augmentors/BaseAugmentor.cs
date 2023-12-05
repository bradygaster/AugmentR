namespace Backend.Augmentors;

public abstract class BaseAugmentor(SemanticKernelWrapper semanticKernelWrapper, ILogger<BaseAugmentor> logger)
{
    protected SemanticKernelWrapper semanticKernelWrapper = semanticKernelWrapper;
    protected ILogger<BaseAugmentor> logger = logger;

    public abstract Task Load();
}
