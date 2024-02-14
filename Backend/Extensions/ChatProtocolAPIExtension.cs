namespace Microsoft.AspNetCore.Builder;

public static class ChatProtocolAPIExtension
{
    public static IEndpointRouteBuilder AddChatProtocolApis(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/", () =>
        {

        });

        builder.MapPost("chat", () =>
        {

        });

        builder.MapPost("ask", () =>
        { 

        });

        return builder;
    }
}
