namespace Backend.Models;

/// <summary>
/// An object containing the "memory" for the chat app, such as a user ID.
/// </summary>
/// <param name="UserId">The user's identifier.</param>
public record class SessionState(
    string UserId);