namespace Backend.Models;

public record class Usage(
    int CompletionTokens,
    int PromptTokens,
    int TotalTokens);
