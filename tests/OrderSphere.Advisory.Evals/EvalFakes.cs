using System.Runtime.CompilerServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using MockQueryable.NSubstitute;
using NSubstitute;
using OrderSphere.Advisory.Api.Agent;
using OrderSphere.Advisory.Application.Abstractions;
using OrderSphere.Advisory.Domain.Entities;

namespace OrderSphere.Advisory.Evals;

internal sealed class FakeChatClientFactory(IChatClient? client) : IAdvisorChatClientFactory
{
    public IChatClient? GetChatClient() => client;
}

internal sealed class FakeToolSource(params AITool[] tools) : IAdvisorToolSource
{
    public Task<AdvisorToolLease> AcquireAsync(CancellationToken ct)
        => Task.FromResult(new AdvisorToolLease([.. tools], Connection: null));
}

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

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var turn = _turns.Dequeue();
        foreach (var item in turn)
        {
            if (item.Throw) throw new InvalidOperationException("Scripted failure.");
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
    public void Dispose() { }
}

internal static class EvalServiceFactory
{
    internal static IAdvisoryDbContext EmptyDb()
    {
        var conversations = Array.Empty<Conversation>().BuildMockDbSet();
        var messages = Array.Empty<ConversationMessage>().BuildMockDbSet();
        var db = Substitute.For<IAdvisoryDbContext>();
        db.Conversations.Returns(conversations);
        db.ConversationMessages.Returns(messages);
        db.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        return db;
    }

    internal static AdvisorChatService Build(IChatClient? chatClient, IAdvisorToolSource? toolSource = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim("sub", "eval-user")], authenticationType: "test"));
        var accessor = new HttpContextAccessor { HttpContext = ctx };

        return new AdvisorChatService(
            accessor,
            EmptyDb(),
            new FakeChatClientFactory(chatClient),
            toolSource ?? new FakeToolSource(),
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AdvisorChatService>.Instance);
    }
}
