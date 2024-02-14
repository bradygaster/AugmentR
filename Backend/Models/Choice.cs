namespace Backend.Models;

public record class Choice(
    ContentFilterResults ContentFilterResults,
    Context Context,
    string FinishReason,
    int Index,
    Message Message,
    string? SessionState);
