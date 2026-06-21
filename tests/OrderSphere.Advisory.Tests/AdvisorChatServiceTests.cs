using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.NSubstitute;
using NSubstitute;
using OrderSphere.Advisory.Api.Agent;
using OrderSphere.Advisory.Application.Abstractions;
using OrderSphere.Advisory.Domain.Entities;
using Xunit;

namespace OrderSphere.Advisory.Tests;

public sealed class AdvisorChatServiceTests
{
    private const string Sub = "user-1";
    private const string ConversationId = "conv-1";


    private static IHttpContextAccessor Accessor(bool authenticated)
    {
        var ctx = new DefaultHttpContext();
        if (authenticated)
        {
            ctx.User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim("sub", Sub)], authenticationType: "test"));
        }
        return new HttpContextAccessor { HttpContext = ctx };
    }

    private static IAdvisoryDbContext Db(params Conversation[] existing)
    {
        var conversations = existing.BuildMockDbSet();
        var messages = Array.Empty<ConversationMessage>().BuildMockDbSet();
        var db = Substitute.For<IAdvisoryDbContext>();
        db.Conversations.Returns(conversations);
        db.ConversationMessages.Returns(messages);
        db.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        return db;
    }

    private static AdvisorChatService Service(
        IChatClient? chatClient,
        IAdvisoryDbContext db,
        bool authenticated = true,
        IAdvisorToolSource? toolSource = null)
        => new(
            Accessor(authenticated),
            db,
            new FakeChatClientFactory(chatClient),
            toolSource ?? new FakeToolSource(),
            NullLogger<AdvisorChatService>.Instance);

    private static async Task<List<AdvisorStreamEvent>> CollectAsync(
        AdvisorChatService service, string message = "Hallo")
    {
        List<AdvisorStreamEvent> events = [];
        await foreach (var evt in service.StreamAsync(ConversationId, message))
        {
            events.Add(evt);
        }
        return events;
    }

    private static string TextOf(IEnumerable<AdvisorStreamEvent> events)
        => string.Concat(events
            .Where(e => e.Kind == AdvisorStreamEventKind.Text)
            .Select(e => e.Text));


    [Fact]
    public async Task Stream_WithoutFoundryConfig_YieldsFriendlyMessage_AndDoesNotPersist()
    {
        var db = Db();
        var service = Service(chatClient: null, db);

        var events = await CollectAsync(service);

        events.Should().ContainSingle().Which.Text.Should().Contain("nicht konfiguriert");
        await db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }


    [Fact]
    public async Task Stream_AuthenticatedTurn_StreamsText_AndPersistsTranscript()
    {
        var db = Db();
        Conversation? added = null;
        db.Conversations.When(s => s.Add(Arg.Any<Conversation>()))
            .Do(ci => added = ci.Arg<Conversation>());

        var client = new ScriptedChatClient().AddTextTurn("Hal", "lo!");
        var service = Service(client, db);

        var events = await CollectAsync(service);

        TextOf(events).Should().Be("Hallo!");
        added.Should().NotBeNull();
        added!.CustomerSub.Should().Be(Sub);
        added.ConversationKey.Should().Be(ConversationId);
        added.SerializedSession.Should().NotBeNullOrEmpty();
        added.Messages.Should().HaveCount(2);
        added.Messages[0].Role.Should().Be("user");
        added.Messages[1].Role.Should().Be("assistant");
        added.Messages[1].Text.Should().Be("Hallo!");
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Stream_AnonymousTurn_RunsEphemerally_WithoutPersistence()
    {
        var db = Db();
        var client = new ScriptedChatClient().AddTextTurn("Hallo!");
        var service = Service(client, db, authenticated: false);

        var events = await CollectAsync(service);

        TextOf(events).Should().Be("Hallo!");
        db.Conversations.DidNotReceive().Add(Arg.Any<Conversation>());
        await db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }


    [Fact]
    public async Task Stream_CorruptStoredSession_StartsFresh_InsteadOfFailing()
    {
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            ConversationKey = ConversationId,
            CustomerSub = Sub,
            SerializedSession = "{this is not valid json"
        };
        var db = Db(conversation);
        var client = new ScriptedChatClient().AddTextTurn("Weiter geht's.");
        var service = Service(client, db);

        var events = await CollectAsync(service);

        TextOf(events).Should().Be("Weiter geht's.");
        // The corrupt state was replaced by a freshly serialized session.
        conversation.SerializedSession.Should().NotContain("this is not valid json");
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Stream_MidStreamFailure_YieldsFriendlyMessage_AndDoesNotPersist()
    {
        var db = Db();
        var client = new ScriptedChatClient().AddFailingTurn("Erste");
        var service = Service(client, db);

        var events = await CollectAsync(service);

        events.Last().Text.Should().Contain("Entschuldigung");
        await db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Stream_McpUnreachable_YieldsFriendlyMessage_InsteadOfThrowing()
    {
        var db = Db();
        var client = new ScriptedChatClient().AddTextTurn("unreachable");
        var service = Service(client, db, toolSource: new ThrowingToolSource());

        var events = await CollectAsync(service);

        events.Should().ContainSingle().Which.Text.Should().Contain("Entschuldigung");
        await db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }


    [Fact]
    public async Task Stream_FunctionCall_EmitsToolEvent_WithGermanLabel()
    {
        var db = Db();
        var searchTool = AIFunctionFactory.Create(
            (string query) => """{"count":0,"products":[]}""",
            name: "search_products");

        var client = new ScriptedChatClient()
            .AddFunctionCallTurn("call-1", "search_products",
                new Dictionary<string, object?> { ["query"] = "trail" })
            .AddTextTurn("Nichts gefunden.");

        var service = Service(client, db, toolSource: new FakeToolSource(searchTool));

        var events = await CollectAsync(service, "Zeig mir Trail-Schuhe");

        events.Should().Contain(e =>
            e.Kind == AdvisorStreamEventKind.Tool && e.Text == "Durchsuche den Katalog");
        TextOf(events).Should().Be("Nichts gefunden.");
    }
}
