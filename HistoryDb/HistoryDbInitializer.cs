internal sealed class HistoryDbInitializer(IServiceProvider serviceProvider, 
    ILogger<HistoryDbInitializer> logger) : BackgroundService
{
    public const string ActivitySourceName = "Migrations";
    private readonly ActivitySource _activitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HistoryDbContext>();

        await InitializeDatabaseAsync(dbContext, stoppingToken);
    }

    private async Task InitializeDatabaseAsync(HistoryDbContext dbContext, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        using var activity = _activitySource.StartActivity("Migrating history database", ActivityKind.Client);

        var sw = Stopwatch.StartNew();

        await strategy.ExecuteAsync(() => dbContext.Database.MigrateAsync(cancellationToken));

        await SeedAsync(dbContext, cancellationToken);

        logger.LogInformation("Database initialization completed after {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
    }

    private async Task SeedAsync(HistoryDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!dbContext.HistoryItems.Any())
        {
            await dbContext.HistoryItems.AddAsync(new HistoryItem
            {
                Timestamp = DateTime.UtcNow,
                Type = "System",
                SourceUrl = "None",
                Description = "History database created",
                ContentId = "None"
            }, cancellationToken);

            logger.LogInformation("Seeding history table");

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Seeded history table");
        }
    }
}
