using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using OrderSphere.Advisory.Api.Agent;

namespace OrderSphere.Advisory.Tests;

/// <summary>Factory stub: returns a fixed chat client (or null = "not configured").</summary>
internal sealed class FakeChatClientFactory(IChatClient? client) : IAdvisorChatClientFactory
{
    public IChatClient? GetChatClient() => client;
}

/// <summary>Tool source stub: hands out a fixed tool list without any MCP connection.</summary>
internal sealed class FakeToolSource(params AITool[] tools) : IAdvisorToolSource
{
    public Task<AdvisorToolLease> AcquireAsync(CancellationToken ct)
        => Task.FromResult(new AdvisorToolLease([.. tools], Connection: null));
}

/// <summary>Tool source stub that fails to connect (simulates the MCP server being down).</summary>
internal sealed class ThrowingToolSource : IAdvisorToolSource
{
    public Task<AdvisorToolLease> AcquireAsync(CancellationToken ct)
        => throw new HttpRequestException("MCP server unreachable.");
}

/// <summary>
/// IChatClient returning pre-scripted streaming turns. Each call to
/// GetStreamingResponseAsync consumes the next scripted turn, which lets tests
/// drive multi-step flows (function call → final answer) and mid-stream failures.
/// </summary>
internal sealed class ScriptedChatClient : IChatClient
{
    private readonly Queue<IReadOnlyList<ScriptedItem>> _turns = new();

    private sealed record ScriptedItem(ChatResponseUpdate? Update, bool Throw);

    public ScriptedChatClient AddTextTurn(params string[] tokens)
    {
        _turns.Enqueue([.. tokens.Select(t =>
            new ScriptedItem(new ChatResponseUpdate(ChatRole.Assistant, t), Throw: false))]);
        return this;
    }

    public ScriptedChatClient AddFunctionCallTurn(string callId, string name,
        IDictionary<string, object?>? arguments = null)
    {
        _turns.Enqueue([new ScriptedItem(
            new ChatResponseUpdate(ChatRole.Assistant, [new FunctionCallContent(callId, name, arguments)]),
            Throw: false)]);
        return this;
    }

    /// <summary>
    /// Enqueues a turn from arbitrary updates, so a test can mix text, function
    /// calls, function results and usage content within a single streamed turn.
    /// </summary>
    public ScriptedChatClient AddTurn(params ChatResponseUpdate[] updates)
    {
        _turns.Enqueue([.. updates.Select(u => new ScriptedItem(u, Throw: false))]);
        return this;
    }

    public ScriptedChatClient AddFailingTurn(params string[] tokensBeforeFailure)
    {
        List<ScriptedItem> items = [.. tokensBeforeFailure.Select(t =>
            new ScriptedItem(new ChatResponseUpdate(ChatRole.Assistant, t), Throw: false))];
        items.Add(new ScriptedItem(Update: null, Throw: true));
        _turns.Enqueue(items);
        return this;
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var turn = _turns.Dequeue();
        foreach (var item in turn)
        {
            if (item.Throw)
            {
                throw new InvalidOperationException("Scripted mid-stream failure.");
            }
            await Task.Yield();
            yield return item.Update!;
        }
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => GetStreamingResponseAsync(messages, options, cancellationToken)
            .ToChatResponseAsync(cancellationToken: cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
