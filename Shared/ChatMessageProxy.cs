namespace Shared;

public class ChatMessageProxy
{
    [JsonPropertyName("isUserMessage")]
    public bool IsUserMessage { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}
