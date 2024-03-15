namespace Backend.Services;

public class LiveUpdateService(IConnectionMultiplexer redis)
{
    private readonly ISubscriber subscriber = redis.GetSubscriber();

    public async Task ShowSystemUpdate(string status) =>
        await subscriber.PublishAsync(new RedisChannel(nameof(Status), RedisChannel.PatternMode.Auto), 
            JsonSerializer.Serialize(new Status(status)));
}
