using Microsoft.AspNetCore.Http.Features;
using OrderSphere.Advisory.Api.Configuration;

namespace OrderSphere.Advisory.Api.Agent;

public sealed record ChatRequest(string? ConversationId, string Message);

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapAdvisorEndpoints(this IEndpointRouteBuilder app)
    {
        // Streams the assistant reply as Server-Sent Events. Each token delta is one
        // `data:` frame; a final `data: [DONE]` frame signals completion.
        app.MapPost("/chat", async (
            ChatRequest request,
            AdvisorChatService chat,
            HttpContext http,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                http.Response.StatusCode = StatusCodes.Status400BadRequest;
                await http.Response.WriteAsync("Message must not be empty.", ct);
                return;
            }

            // Disable response buffering so tokens flush to the client as they arrive.
            http.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
            http.Response.ContentType = "text/event-stream";
            http.Response.Headers.CacheControl = "no-cache";

            var conversationId = string.IsNullOrWhiteSpace(request.ConversationId)
                ? Guid.NewGuid().ToString("n")
                : request.ConversationId;

            await foreach (var chunk in chat.StreamAsync(conversationId, request.Message, ct))
            {
                var payload = chunk.Replace("\r", string.Empty).Replace("\n", "\ndata: ");
                await http.Response.WriteAsync($"data: {payload}\n\n", ct);
                await http.Response.Body.FlushAsync(ct);
            }

            await http.Response.WriteAsync("data: [DONE]\n\n", ct);
            await http.Response.Body.FlushAsync(ct);
        })
        .RequireRateLimiting(RateLimitingExtensions.ChatPolicy);

        return app;
    }
}
