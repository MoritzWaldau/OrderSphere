using Microsoft.AspNetCore.Http.Features;
using OrderSphere.Advisory.Api.Configuration;

namespace OrderSphere.Advisory.Api.Agent;

public sealed record ChatRequest(string? ConversationId, string Message);

public static class ChatEndpoints
{
    public static RouteGroupBuilder MapAdvisorChatEndpoints(this RouteGroupBuilder group)
    {
        // Streams the assistant reply as Server-Sent Events. Each text token delta is
        // one unnamed `data:` frame; tool activity is a named `event: tool` frame whose
        // data carries a user-facing label; a final `data: [DONE]` frame signals completion.
        group.MapPost("/chat", async (
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

            await foreach (var evt in chat.StreamAsync(conversationId, request.Message, ct))
            {
                var payload = evt.Text.Replace("\r", string.Empty).Replace("\n", "\ndata: ");
                var frame = evt.Kind switch
                {
                    AdvisorStreamEventKind.Tool => $"event: tool\ndata: {payload}\n\n",
                    AdvisorStreamEventKind.Confirm => $"event: confirm\ndata: {payload}\n\n",
                    AdvisorStreamEventKind.Citation => $"event: citation\ndata: {payload}\n\n",
                    _ => $"data: {payload}\n\n"
                };
                await http.Response.WriteAsync(frame, ct);
                await http.Response.Body.FlushAsync(ct);
            }

            await http.Response.WriteAsync("data: [DONE]\n\n", ct);
            await http.Response.Body.FlushAsync(ct);
        })
        .RequireRateLimiting(RateLimitingExtensions.ChatPolicy);

        return group;
    }
}
