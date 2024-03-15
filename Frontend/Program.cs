var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddAzureOpenAIClient("openai");
builder.AddAzureQueueClient("AzureQueues");
builder.AddRedisClient("pubsub");
builder.Services.AddSingleton<LiveUpdateSubscriber>();
builder.Services.AddSingleton<LiveUpdateFrontEndMessenger>();
builder.Services.AddHostedService<LiveUpdateSubscriberHostedService>();
builder.Services.AddHttpClient<HistoryApiClient>(client => client.BaseAddress = new("http://historyservice"));
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();