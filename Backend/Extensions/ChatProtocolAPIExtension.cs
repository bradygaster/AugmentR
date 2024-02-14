using Backend.Models;

namespace Microsoft.AspNetCore.Builder;

public static class ChatProtocolApiExtension
{
    public static IEndpointRouteBuilder AddChatProtocolApis(this IEndpointRouteBuilder builder)
    {
        // Expose chat protocol APIs:
        //   GET  /v1/
        //   POST /v1/chat
        //   POST /v1/ask
        var v1 = builder.MapGroup("v1");

        v1.MapGet("/", static () =>
        {

        });

        v1.MapPost("/chat", static (ChatRequest request) =>
        {
            // TODO: retrun new ChatResponse();
        });

        v1.MapPost("/ask", static () =>
        { 

        });

        return builder;
    }
}
