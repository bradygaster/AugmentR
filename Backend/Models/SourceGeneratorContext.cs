namespace Backend.Models;

[JsonSourceGenerationOptions(
    defaults: JsonSerializerDefaults.Web,
    AllowTrailingCommas = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(ChatRequest))]
[JsonSerializable(typeof(ChatResponse))]
public sealed partial class SourceGeneratorContext : JsonSerializerContext
{
}
