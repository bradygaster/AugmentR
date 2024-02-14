namespace Frontend.Services;

public sealed class LiveUpdateSubscriber(
    ILogger<LiveUpdateSubscriber> logger,
    IConnectionMultiplexer redis) : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        if (redis != null && redis.IsConnected)
        {
            var subscriber = redis.GetSubscriber();

            await subscriber.UnsubscribeAsync(
                new RedisChannel(nameof(Status),
                RedisChannel.PatternMode.Auto));

            await redis.DisposeAsync();
        }
    }

    public async Task SubscribeAsync(Action<Status?> action, CancellationToken cancellationToken)
    {
        var subscriber = redis.GetSubscriber();

        await subscriber.SubscribeAsync(
            channel: new RedisChannel(nameof(Status), RedisChannel.PatternMode.Auto),
            handler: (ch, value) =>
            {
                if (value.HasValue is false)
                {
                    return;
                }

                var status = JsonSerializer.Deserialize<Status>(value.ToString());
                
                action.Invoke(status);
            });
    }
}
