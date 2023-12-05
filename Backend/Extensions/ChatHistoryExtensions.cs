namespace Microsoft.SemanticKernel.AI.ChatCompletion;

public static class ChatHistoryExtensions
{
    public static List<ChatMessageProxy> ToProxy(this ChatHistory chathistory)
    {
        return chathistory.Select(m => new ChatMessageProxy
        {
            IsUserMessage = m.Role == AuthorRole.User,
            Content = m.Content
        }).ToList();
    }

    public static ChatHistory FromProxy(this List<ChatMessageProxy> proxies)
    {
        var chatHistory = new ChatHistory();

        foreach (var item in proxies)
        {
            if (item.IsUserMessage)
            {
                chatHistory.AddUserMessage(item.Content);
            }
            else
            {
                chatHistory.AddAssistantMessage(item.Content);
            }
        }

        return chatHistory;
    }
}
