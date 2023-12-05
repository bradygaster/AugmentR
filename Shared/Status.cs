namespace Shared;

public record Status(string? Summary = "System is up")
{
    public DateTime LastUpdated { get; } = DateTime.UtcNow;
}
