using FluentAssertions;
using OrderSphere.Advisory.Api.Agent;
using Xunit;

namespace OrderSphere.Advisory.Evals;

/// <summary>
/// Deterministic (no Foundry required): verifies that the agent service surfaces
/// the correct tool-label SSE events when the scripted LLM returns specific function calls.
/// These tests always run in CI.
/// </summary>
public sealed class ToolSelectionEvalTests
{
    private static async Task<List<AdvisorStreamEvent>> CollectAsync(AdvisorChatService svc, string msg)
    {
        var events = new List<AdvisorStreamEvent>();
        await foreach (var e in svc.StreamAsync("eval-conv", msg))
            events.Add(e);
        return events;
    }

    public static IEnumerable<object[]> ToolLabelCases =>
    [
        ["search_products", "Durchsuche den Katalog"],
        ["get_product", "Rufe Produktdetails ab"],
        ["list_categories", "Lade Kategorien"],
        ["get_my_cart", "Schaue in deinen Warenkorb"],
        ["get_my_orders", "Rufe deine Bestellungen ab"],
        ["add_to_cart", "Lege in den Warenkorb"],
        ["get_similar_products", "Suche ähnliche Produkte"],
    ];

    [Theory]
    [MemberData(nameof(ToolLabelCases))]
    public async Task Service_EmitsCorrectToolLabel_ForEachTool(string toolName, string expectedLabel)
    {
        var client = new ScriptedChatClient()
            .AddFunctionCallTurn("id-1", toolName)
            .AddTextTurn("Ergebnis.");

        var svc = EvalServiceFactory.Build(client);
        var events = await CollectAsync(svc, "Frage");

        events.Should().Contain(e => e.Kind == AdvisorStreamEventKind.Tool && e.Text == expectedLabel);
    }

    [Fact]
    public async Task Service_EmitsToolEvent_BeforeTextEvents()
    {
        var client = new ScriptedChatClient()
            .AddFunctionCallTurn("id-1", "search_products")
            .AddTextTurn("Hier sind die Ergebnisse.");

        var svc = EvalServiceFactory.Build(client);
        var events = await CollectAsync(svc, "Sneaker unter 100 Euro");

        var toolIdx = events.FindIndex(e => e.Kind == AdvisorStreamEventKind.Tool);
        var textIdx = events.FindIndex(e => e.Kind == AdvisorStreamEventKind.Text);

        toolIdx.Should().BeGreaterThanOrEqualTo(0);
        textIdx.Should().BeGreaterThan(toolIdx);
    }

    [Fact]
    public async Task Service_DoesNotDuplicateToolEvent_ForSameTool()
    {
        // Both updates carry the same tool name — only one Tool event should be emitted.
        var client = new ScriptedChatClient()
            .AddFunctionCallTurn("id-1", "search_products")
            .AddFunctionCallTurn("id-2", "search_products")
            .AddTextTurn("Fertig.");

        var svc = EvalServiceFactory.Build(client);
        var events = await CollectAsync(svc, "Frage");

        events.Count(e => e.Kind == AdvisorStreamEventKind.Tool).Should().Be(1);
    }

    [Fact]
    public async Task Service_EmitsMultipleToolEvents_ForDistinctTools()
    {
        var client = new ScriptedChatClient()
            .AddFunctionCallTurn("id-1", "search_products")
            .AddFunctionCallTurn("id-2", "get_product")
            .AddTextTurn("Fertig.");

        var svc = EvalServiceFactory.Build(client);
        var events = await CollectAsync(svc, "Frage");

        events.Count(e => e.Kind == AdvisorStreamEventKind.Tool).Should().Be(2);
    }

    [Fact]
    public async Task Service_EmitsConfirmEvent_WhenLlmReturnsConfirmPayload()
    {
        const string payload = """{"__confirm__":"add_to_cart","slug":"sneaker-x","quantity":1,"productName":"Sneaker X","unitPrice":89.99,"summary":"Sneaker X (1×) für 89,99 €"}""";

        var client = new ScriptedChatClient()
            .AddTextTurn(payload);

        var svc = EvalServiceFactory.Build(client);
        var events = await CollectAsync(svc, "Sneaker X in den Warenkorb");

        events.Should().ContainSingle(e => e.Kind == AdvisorStreamEventKind.Confirm);
        events.Should().NotContain(e => e.Kind == AdvisorStreamEventKind.Text);
    }
}
