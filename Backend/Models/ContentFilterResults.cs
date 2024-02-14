namespace Backend.Models;

public record class ContentFilterResults(
    FilterResult Hate,
    FilterResult SelfHarm,
    FilterResult Sexual,
    FilterResult Violence);
