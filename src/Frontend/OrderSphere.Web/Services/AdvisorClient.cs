using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace OrderSphere.Web.Services;

public interface IAdvisorClient
{
    // Streams the assistant reply token-by-token. conversationId keeps context
    // across turns; pass the same value back on follow-up messages.
    IAsyncEnumerable<string> StreamAsync(string conversationId, string message, CancellationToken ct = default);
}

public sealed class AdvisorClient(HttpClient client) : IAdvisorClient
{
    public async IAsyncEnumerable<string> StreamAsync(
        string conversationId,
        string message,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/advisor/chat")
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

        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null)
            {
                yield break;
            }
            if (line.Length == 0 || !line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            var data = line[5..].TrimStart();
            if (data == "[DONE]")
            {
                yield break;
            }

            yield return data;
        }
    }
}
