namespace Frontend.Workers;

public class LiveUpdateSubscriberHostedService(
    ILogger<LiveUpdateSubscriberHostedService> logger,
    LiveUpdateSubscriber subscriber,
    LiveUpdateFrontEndMessenger messenger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await subscriber.SubscribeAsync((status) =>
        {
            if (status is null)
            {
                logger.LogWarning("Status is null");
                return;
            }

            logger.LogInformation("Status at {LastUpdated}: {Summary}", 
                status.LastUpdated, status.Summary);

            messenger.Notify(status);
        }, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await subscriber.DisposeAsync();
    }
}
