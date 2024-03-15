namespace Backend.Workers;

public class AugmentationWorker(
    SemanticKernelWrapper semanticKernelClient,
    ILogger<AugmentationWorker> logger,
    UrlAugmentor urlAugmentor,
    UrlListAugmentor urlListAugmentor,
    LiveUpdateService liveUpdateService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(5_000, stoppingToken);

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

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await urlAugmentor.OnStarted();
        await urlListAugmentor.OnStarted();
    }
}
