namespace Frontend.Services;

public class LiveUpdateSubscriber(ILogger<LiveUpdateSubscriber> logger,
        IConnectionMultiplexer redis) : IDisposable
{
    private readonly ILogger<LiveUpdateSubscriber> logger = logger;
    private readonly IConnectionMultiplexer redis = redis;

    public async void Dispose()
    {
        if (redis != null && redis.IsConnected)
        {
            var subscriber = redis.GetSubscriber();
            await subscriber.UnsubscribeAsync(new RedisChannel(nameof(Status),
                               RedisChannel.PatternMode.Auto));
            redis.Dispose();
        }
    }

    public async Task SubscribeAsync(CancellationToken cancellationToken, Action<Status> action)
    {
        var subscriber = redis.GetSubscriber();
        await subscriber.SubscribeAsync(new RedisChannel(nameof(Status),
                RedisChannel.PatternMode.Auto), (ch, value) =>
                {
#pragma warning disable CS8604 // Possible null reference argument.
                    var obj = JsonSerializer.Deserialize<Status>(value);
                    action(obj);
#pragma warning restore CS8604 // Possible null reference argument.
                });
    }
}
