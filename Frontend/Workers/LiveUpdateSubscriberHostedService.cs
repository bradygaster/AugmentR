
namespace Frontend.Workers;

public class LiveUpdateSubscriberHostedService(ILogger<LiveUpdateSubscriberHostedService> logger,
        LiveUpdateSubscriber subscriber,
        LiveUpdateFrontEndMessenger messenger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await subscriber.SubscribeAsync(cancellationToken, (status) =>
        {
            logger.LogInformation($"Status at {status.LastUpdated}: {status.Summary}");
            messenger.Notify(status);
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        subscriber.Dispose();
        return Task.CompletedTask;
    }
}
