var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<HistoryDbContext>("historydb");

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapHistoryApi();
app.Run();