namespace Backend.Models;

/// <summary>
/// See <a href="https://github.com/anfibiacreativa/ai-chat-app-protocol?tab=readme-ov-file#http-requests-to-ai-chat-app-endpoints"></a>
/// </summary>
/// <param name="Messages">A list of messages, each containing "content" and "role", 
/// where "role" may be "assistant" or "user". A single-turn chat app may only contain 
/// 1 message, while a multi-turn chat app may contain multiple messages.</param>
/// <param name="Stream">A boolean indicating whether the response should be streamed or not.</param>
/// <param name="Context">Optional. An object containing any additional context about the 
/// request, such as the temperature to use for the LLM. Each application may define its 
/// own context properties.</param>
/// <param name="SessionState">Optional. An object containing the "memory" for the chat app, such as a user ID.</param>
public record class ChatRequest(
    ChatMessage[] Messages,
    bool Stream,
    ChatContext? Context = null,
    SessionState? SessionState = null);
