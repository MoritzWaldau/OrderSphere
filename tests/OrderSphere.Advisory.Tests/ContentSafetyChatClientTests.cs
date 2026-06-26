using FluentAssertions;
using Microsoft.Extensions.AI;
using OrderSphere.Advisory.Api.Agent;
using Xunit;

namespace OrderSphere.Advisory.Tests;

public sealed class ContentSafetyChatClientTests
{
    private static async Task<List<ChatResponseUpdate>> CollectAsync(
        IChatClient client, string userMessage)
    {
        var messages = new List<ChatMessage> { new(ChatRole.User, userMessage) };
        var updates = new List<ChatResponseUpdate>();
        await foreach (var u in client.GetStreamingResponseAsync(messages))
            updates.Add(u);
        return updates;
    }

    [Fact]
    public async Task BlockedInput_ReturnsRefusal_WithoutCallingInner()
    {
        // Inner has no scripted turns; if called it would throw (empty queue).
        var inner = new ScriptedChatClient();
        var sut = new ContentSafetyChatClient(inner, (_, _) => Task.FromResult(true));

        var updates = await CollectAsync(sut, "harmful content");

        updates.Should().ContainSingle(u => !string.IsNullOrWhiteSpace(u.Text));
    }

    [Fact]
    public async Task SafeInput_DelegatesToInner()
    {
        var inner = new ScriptedChatClient().AddTextTurn("Hello from inner.");
        var sut = new ContentSafetyChatClient(inner, (_, _) => Task.FromResult(false));

        var updates = await CollectAsync(sut, "normal question");

        updates.Should().ContainSingle(u => u.Text == "Hello from inner.");
    }

    [Fact]
    public async Task NoUserMessage_SkipsCheck_AndDelegatesToInner()
    {
        var inner = new ScriptedChatClient().AddTextTurn("Response.");
        var checkerCalled = false;
        var sut = new ContentSafetyChatClient(inner, (_, _) =>
        {
            checkerCalled = true;
            return Task.FromResult(false);
        });

        var messages = new List<ChatMessage> { new(ChatRole.System, "System context only.") };
        var updates = new List<ChatResponseUpdate>();
        await foreach (var u in sut.GetStreamingResponseAsync(messages))
            updates.Add(u);

        checkerCalled.Should().BeFalse();
        updates.Should().ContainSingle(u => u.Text == "Response.");
    }

    [Fact]
    public async Task BlockedRefusal_ContainsNoInternalIds()
    {
        var inner = new ScriptedChatClient();
        var sut = new ContentSafetyChatClient(inner, (_, _) => Task.FromResult(true));

        var updates = await CollectAsync(sut, "blocked");

        var text = string.Concat(updates.Select(u => u.Text));
        text.Should().NotMatchRegex(
            @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
            because: "refusal messages must not expose internal identifiers");
    }

    [Fact]
    public async Task MultiTurnConversation_ChecksOnlyLastUserMessage()
    {
        var checkedTexts = new List<string>();
        var inner = new ScriptedChatClient().AddTextTurn("OK.");
        var sut = new ContentSafetyChatClient(inner, (text, _) =>
        {
            checkedTexts.Add(text);
            return Task.FromResult(false);
        });

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "first turn"),
            new(ChatRole.Assistant, "first reply"),
            new(ChatRole.User, "second turn"),
        };

        var updates = new List<ChatResponseUpdate>();
        await foreach (var u in sut.GetStreamingResponseAsync(messages))
            updates.Add(u);

        checkedTexts.Should().ContainSingle("second turn");
    }
}
