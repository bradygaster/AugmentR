public record NewHistoricalItem(string ContentId,
    string SourceUrl,
    string SourceType,
    string Description);

public class HistoricalItem
{
    [JsonPropertyName("contentId")]
    public string ContentId { get; set; } = string.Empty;

    [JsonPropertyName("sourceUrl")]
    public string SourceUrl { get; set; } = string.Empty;

    [JsonPropertyName("sourceType")]
    public string SourceType { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}