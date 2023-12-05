namespace Backend.Workers;

public class AugmentationWorker(SemanticKernelWrapper semanticKernelClient,
        ILogger<AugmentationWorker> logger,
        IConfiguration configuration,
        UrlAugmentor urlAugmentor,
        UrlListAugmentor urlListAugmentor,
        LiveUpdateService liveUpdateService) : BackgroundService
{
    private SemanticKernelWrapper semanticKernelClient = semanticKernelClient;
    private ILogger<AugmentationWorker> logger = logger;
    private readonly IConfiguration configuration = configuration;
    private readonly UrlAugmentor urlAugmentor = urlAugmentor;
    private readonly UrlListAugmentor urlListAugmentor = urlListAugmentor;
    private readonly LiveUpdateService liveUpdateService = liveUpdateService;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(5000);

            try
            {
                if (!semanticKernelClient.IsInitialized())
                {
                    await semanticKernelClient.InitializeKernel();
                }
                else
                {
                    logger.LogInformation("Semantic Kernel client is already initialized.");
                }

                await urlAugmentor.Load();
                await urlListAugmentor.Load();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error initializing Semantic Kernel client.");
            }

            await liveUpdateService.ShowSystemUpdate("System is up");
        }
    }
}
