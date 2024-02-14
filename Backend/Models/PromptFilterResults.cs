namespace Backend.Models;

public record class PromptFilterResults(
    ContentFilterResults ContentFilterResults,
    int PromptIndex);
