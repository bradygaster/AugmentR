namespace Backend.Models;

public record class Thought(
    string? Description,
    Props Props,
    string Title);
