public static class HistoryApiWebApplicationExtensions
{
    public static IEndpointRouteBuilder MapHistoryApi(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/history",
            async (NewHistoricalItem historyItem,
            HistoryDbContext historyDbContext) =>
            {
                await historyDbContext.AddAsync(
                    new HistoryItem
                    {
                        ContentId = historyItem.ContentId,
                        SourceUrl = historyItem.SourceUrl,
                        Type = historyItem.SourceType,
                        Timestamp = DateTime.UtcNow,
                        Description =
                            historyItem.Description,
                    });

                await historyDbContext.SaveChangesAsync();
            })
        .WithName("SaveNewHistoryItem")
        .WithOpenApi();

        builder.MapGet("/history", (HistoryDbContext historyDbContext) =>
            {
                var historyItems = historyDbContext.HistoryItems
                    .OrderByDescending(hi => hi.Timestamp)
                    .ToList()
                        .Select(hi =>
                            new HistoricalItem
                            {
                                ContentId = hi.ContentId,
                                SourceUrl = hi.SourceUrl,
                                SourceType = hi.Type,
                                Description = hi.Description,
                                Timestamp = hi.Timestamp
                            })
                            .ToArray();

                return Results.Ok(historyItems);
            })
        .WithName("GetHistoryItems")
        .WithOpenApi();

        return builder;
    }
}
