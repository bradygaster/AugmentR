var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureQueueClient("AzureQueues");
builder.AddAzureBlobClient("AzureBlobs");
builder.AddRedisClient("pubsub");
builder.Services.AddHttpClient<HistoryApiClient>(client => client.BaseAddress = new ("http://historyservice"));
builder.Services.AddSingleton<SemanticKernelWrapper>();
builder.Services.AddSingleton<UrlAugmentor>();
builder.Services.AddSingleton<UrlListAugmentor>();
builder.Services.AddSingleton<LiveUpdateService>();
builder.Services.AddHostedService<AugmentationWorker>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
}

app.MapGet("/", () => Results.Ok("Backend is up"))
   .WithName("IsUp")
   .WithOpenApi();

app.MapPost("/api/chat", async (List<ChatMessageProxy> chatHistory, SemanticKernelWrapper wrapper) =>
{
    var results = await wrapper.Chat(chatHistory.FromProxy());
    return results.ToProxy();
});

app.Run();
