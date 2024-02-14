namespace Backend.Models;

/// <summary>
/// An object containing settings for the chat application.
/// </summary>
/// <param name="Temperature">The temperature to use for the LLM.</param>
/// <param name="Top">The number of results to return from the search engine.</param>
/// <param name="RetrievalMode">The mode to use for the search engine. Can be "hybrid", "vectors", or "text".</param>
/// <param name="SemanticRanker">Specific to Azure AI Search. Whether to use the semantic ranker for the search engine.</param>
/// <param name="SemanticCaptions">Specific to Azure AI Search. Whether to use semantic captions for the search engine.</param>
/// <param name="SuggestFollowupQuestions">Whether to suggest follow-up questions for the chat app.</param>
/// <param name="UseOidSecurityFilter">Whether to use the OID security filter for the search engine.</param>
/// <param name="UseGroupsSecurityFilter">Whether to use the groups security filter for the search engine.</param>
/// <param name="VectorFields">A list of fields to search for the vector search engine.</param>
/// <param name="UseGpt4v">Whether to use a GPT-4V approach.</param>
/// <param name="Gpt4vInput">The input type to use for a GPT-4V approach. Can be "text", "textAndImages", or "images".</param>
public record class Overrides(
    int? Temperature,
    int? Top,
    string? RetrievalMode,
    bool? SemanticRanker,
    bool? SemanticCaptions,
    bool? SuggestFollowupQuestions,
    bool? UseOidSecurityFilter,
    bool? UseGroupsSecurityFilter,
    string[]? VectorFields,
    bool? UseGpt4v,
    string? Gpt4vInput);
