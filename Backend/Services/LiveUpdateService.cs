namespace Backend.Services;

public class LiveUpdateService(ILogger<LiveUpdateService> logger,
        IConnectionMultiplexer redis)
{
    private ILogger<LiveUpdateService> logger = logger;
    private IConnectionMultiplexer redis = redis;
    private ISubscriber subscriber = redis.GetSubscriber();

    public async Task ShowSystemUpdate(string status) =>
        await subscriber.PublishAsync(new RedisChannel(nameof(Status), RedisChannel.PatternMode.Auto), 
            JsonSerializer.Serialize(new Status(status)));
}
