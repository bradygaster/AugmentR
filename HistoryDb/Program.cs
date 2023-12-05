var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<HistoryDbContext>("historydb");
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(HistoryDbInitializer.ActivitySourceName));
builder.Services.AddSingleton<HistoryDbInitializer>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<HistoryDbInitializer>());
builder.Services.AddHealthChecks()
    .AddCheck<HistoryDbInitializerHealthCheck>("HistoryDbInitializer", null);

var app = builder.Build();

app.MapDefaultEndpoints();

app.Run();
