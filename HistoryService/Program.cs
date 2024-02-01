var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<HistoryDbContext>("historydb");
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapHistoryApi();
app.Run();