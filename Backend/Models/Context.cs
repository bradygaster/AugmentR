namespace Backend.Models;

public record class Context(
    DataPoints DataPoints,
    Thought[] Thoughts);
