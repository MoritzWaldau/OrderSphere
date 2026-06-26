using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace OrderSphere.Advisory.Api.Agent;

/// <summary>
/// Outermost chat-client decorator. Checks the last user message against
/// Azure AI Content Safety before forwarding to the inner pipeline. Blocked
/// messages receive a friendly German refusal without calling the inner client.
/// Fails open: if the safety check itself throws, the request proceeds normally.
/// </summary>
public sealed class ContentSafetyChatClient(
    IChatClient innerClient,
    Func<string, CancellationToken, Task<bool>> isBlockedAsync)
    : DelegatingChatClient(innerClient)
{
    private const string BlockedRefusal =
        "Diese Anfrage entspricht nicht den Nutzungsrichtlinien und kann nicht bearbeitet werden. " +
        "Bitte stelle eine andere Frage.";

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var userText = messages
            .LastOrDefault(m => m.Role == ChatRole.User)
            ?.Text;

        if (!string.IsNullOrWhiteSpace(userText) && await isBlockedAsync(userText, cancellationToken))
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, BlockedRefusal);
            yield break;
        }

        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
            yield return update;
    }
}
