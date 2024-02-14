namespace Backend.Models;

/// <summary>
/// An object containing settings for the chat application.
/// See <a href="https://github.com/anfibiacreativa/ai-chat-app-protocol?tab=readme-ov-file#recommended-request-context"></a>
/// </summary>
/// <param name="Overrides">The optional overrides.</param>
public record class ChatContext(
    Overrides? Overrides = null);
