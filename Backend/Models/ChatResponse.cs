namespace Backend.Models;

public record class ChatResponse(
    Choice[] Choices,
    int Created,
    string Id,
    string Model,
    string Object,
    PromptFilterResults[] PromptFilterResults,
    string? SystemFingerprint,
    Usage Usage);
