namespace Backend.Models;

/// <summary>
/// Represents a chat message.
/// </summary>
/// <param name="Content">The content of the chat message.</param>
/// <param name="Role">The role for the chat message.</param>
public record class ChatMessage(
    string Content,
    string Role);
