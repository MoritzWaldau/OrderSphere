using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace OrderSphere.Web.Services;

public sealed record AdvisorConversation(
    string ConversationId, string? LastMessage, DateTime UpdatedAt, int MessageCount);

public sealed record AdvisorHistoryMessage(string Role, string Text, DateTime CreatedAt);

public enum AdvisorStreamItemKind
{
    // A token of assistant text.
    Text,

    // A status notice that the agent is using a tool; Text is a display label.
    Tool,

    // A write-action requires customer confirmation. Text is the raw JSON payload.
    Confirm
}

public sealed record AdvisorStreamItem(AdvisorStreamItemKind Kind, string Text);

// Deserialized from the confirmation_required JSON payload produced by add_to_cart.
public sealed record AdvisorConfirmPayload(
    string Slug,
    int Quantity,
    string ProductName,
    decimal UnitPrice,
    string Summary);

public interface IAdvisorClient
{
    // Streams the assistant reply token-by-token, interleaved with tool-activity
    // notices. conversationId keeps context across turns; pass the same value back
    // on follow-up messages.
    IAsyncEnumerable<AdvisorStreamItem> StreamAsync(string conversationId, string message, CancellationToken ct = default);

    // Lists the current customer's past conversations, most recently updated first.
    Task<IReadOnlyList<AdvisorConversation>> GetConversationsAsync(CancellationToken ct = default);

    // Returns the stored transcript of one conversation, oldest message first.
    Task<IReadOnlyList<AdvisorHistoryMessage>> GetMessagesAsync(string conversationId, CancellationToken ct = default);
}

public sealed class AdvisorClient(IHttpClientFactory factory) : IAdvisorClient
{
    private readonly HttpClient client = factory.CreateClient("advisor");

    public async Task<IReadOnlyList<AdvisorConversation>> GetConversationsAsync(CancellationToken ct = default)
        => await client.GetFromJsonAsync<List<AdvisorConversation>>("/api/v1/advisor/conversations", ct) ?? [];

    public async Task<IReadOnlyList<AdvisorHistoryMessage>> GetMessagesAsync(
        string conversationId, CancellationToken ct = default)
        => await client.GetFromJsonAsync<List<AdvisorHistoryMessage>>(
               $"/api/v1/advisor/conversations/{Uri.EscapeDataString(conversationId)}", ct) ?? [];

    public async IAsyncEnumerable<AdvisorStreamItem> StreamAsync(
        string conversationId,
        string message,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/advisor/chat")
        {
            Content = JsonContent.Create(new { conversationId, message })
        };
        // Enable incremental reading of the response body in the browser.
        request.SetBrowserResponseStreamingEnabled(true);

        using var response = await client.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        // SSE: an optional `event: <name>` line precedes the `data:` line(s) of a
        // frame; a blank line terminates the frame and resets the event name.
        string? eventName = null;

        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null)
            {
                yield break;
            }
            if (line.Length == 0)
            {
                eventName = null;
                continue;
            }
            if (line.StartsWith("event:", StringComparison.Ordinal))
            {
                eventName = line[6..].Trim();
                continue;
            }
            if (!line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            // SSE strips only a single optional space after "data:"; trimming all
            // whitespace would swallow token-leading spaces and run words together.
            var data = line[5..];
            if (data.StartsWith(' '))
            {
                data = data[1..];
            }

            if (data == "[DONE]")
            {
                yield break;
            }

            yield return eventName switch
            {
                "tool" => new AdvisorStreamItem(AdvisorStreamItemKind.Tool, data),
                "confirm" => new AdvisorStreamItem(AdvisorStreamItemKind.Confirm, data),
                _ => new AdvisorStreamItem(AdvisorStreamItemKind.Text, data)
            };
        }
    }
}
