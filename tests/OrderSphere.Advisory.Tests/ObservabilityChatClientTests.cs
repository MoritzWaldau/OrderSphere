using System.Diagnostics.Metrics;
using FluentAssertions;
using Microsoft.Extensions.AI;
using OrderSphere.Advisory.Api.Agent;
using OrderSphere.BuildingBlocks.Diagnostics;
using Xunit;

namespace OrderSphere.Advisory.Tests;

public sealed class ObservabilityChatClientTests
{
    private static async Task<List<ChatResponseUpdate>> CollectAsync(IChatClient client)
    {
        var messages = new List<ChatMessage> { new(ChatRole.User, "Frage") };
        var updates = new List<ChatResponseUpdate>();
        await foreach (var u in client.GetStreamingResponseAsync(messages))
            updates.Add(u);
        return updates;
    }

    [Fact]
    public async Task PlainTextTurn_StreamsThrough_AndRecordsCompletedDuration()
    {
        var inner = new ScriptedChatClient().AddTextTurn("Hallo", " Welt");
        var sut = new ObservabilityChatClient(inner);

        using var metrics = new MetricCollector();
        var updates = await CollectAsync(sut);

        updates.Select(u => u.Text).Should().Equal("Hallo", " Welt");
        metrics.Doubles.Should().ContainSingle(m =>
            m.Instrument == "ordersphere.advisor.turn.duration"
            && m.Tag("outcome") == "completed");
    }

    [Fact]
    public async Task UsageContent_RecordsInputAndOutputTokens()
    {
        var inner = new ScriptedChatClient().AddTurn(
            new ChatResponseUpdate(ChatRole.Assistant, "Antwort"),
            new ChatResponseUpdate(ChatRole.Assistant,
                [new UsageContent(new UsageDetails { InputTokenCount = 120, OutputTokenCount = 45 })]));
        var sut = new ObservabilityChatClient(inner);

        using var metrics = new MetricCollector();
        await CollectAsync(sut);

        metrics.Longs.Should().ContainSingle(m =>
            m.Instrument == "ordersphere.advisor.tokens" && m.Tag("direction") == "input")
            .Which.Value.Should().Be(120);
        metrics.Longs.Should().ContainSingle(m =>
            m.Instrument == "ordersphere.advisor.tokens" && m.Tag("direction") == "output")
            .Which.Value.Should().Be(45);
    }

    [Fact]
    public async Task MultipleUsageContents_AreSummedPerTurn()
    {
        // A tool loop produces several model round-trips, each reporting usage.
        var inner = new ScriptedChatClient().AddTurn(
            new ChatResponseUpdate(ChatRole.Assistant,
                [new UsageContent(new UsageDetails { InputTokenCount = 100, OutputTokenCount = 10 })]),
            new ChatResponseUpdate(ChatRole.Assistant, "Ergebnis"),
            new ChatResponseUpdate(ChatRole.Assistant,
                [new UsageContent(new UsageDetails { InputTokenCount = 50, OutputTokenCount = 20 })]));
        var sut = new ObservabilityChatClient(inner);

        using var metrics = new MetricCollector();
        await CollectAsync(sut);

        metrics.Longs.Where(m => m.Tag("direction") == "input").Sum(m => m.Value).Should().Be(150);
        metrics.Longs.Where(m => m.Tag("direction") == "output").Sum(m => m.Value).Should().Be(30);
    }

    [Fact]
    public async Task SuccessfulToolCall_RecordsSuccessOutcome()
    {
        var inner = new ScriptedChatClient().AddTurn(
            new ChatResponseUpdate(ChatRole.Assistant, [new FunctionCallContent("c1", "search_products")]),
            new ChatResponseUpdate(ChatRole.Tool, [new FunctionResultContent("c1", "ok")]),
            new ChatResponseUpdate(ChatRole.Assistant, "Hier sind Produkte."));
        var sut = new ObservabilityChatClient(inner);

        using var metrics = new MetricCollector();
        await CollectAsync(sut);

        metrics.Longs.Should().ContainSingle(m =>
            m.Instrument == "ordersphere.advisor.tool.invocations"
            && m.Tag("tool") == "search_products"
            && m.Tag("outcome") == "success");
    }

    [Fact]
    public async Task FailedToolCall_RecordsErrorOutcome()
    {
        var inner = new ScriptedChatClient().AddTurn(
            new ChatResponseUpdate(ChatRole.Assistant, [new FunctionCallContent("c2", "get_my_orders")]),
            new ChatResponseUpdate(ChatRole.Tool,
                [new FunctionResultContent("c2", "Fehler")
                {
                    Exception = new InvalidOperationException("downstream 500")
                }]));
        var sut = new ObservabilityChatClient(inner);

        using var metrics = new MetricCollector();
        await CollectAsync(sut);

        metrics.Longs.Should().ContainSingle(m =>
            m.Instrument == "ordersphere.advisor.tool.invocations"
            && m.Tag("tool") == "get_my_orders"
            && m.Tag("outcome") == "error");
    }

    [Fact]
    public async Task MidStreamFailure_RecordsFailedDuration_AndPropagates()
    {
        var inner = new ScriptedChatClient().AddFailingTurn("Teilantwort");
        var sut = new ObservabilityChatClient(inner);

        using var metrics = new MetricCollector();

        var act = async () => await CollectAsync(sut);
        await act.Should().ThrowAsync<InvalidOperationException>();

        metrics.Doubles.Should().ContainSingle(m =>
            m.Instrument == "ordersphere.advisor.turn.duration"
            && m.Tag("outcome") == "failed");
    }

    [Fact]
    public async Task NoUsageContent_RecordsNoTokenMeasurements()
    {
        var inner = new ScriptedChatClient().AddTextTurn("Nur Text");
        var sut = new ObservabilityChatClient(inner);

        using var metrics = new MetricCollector();
        await CollectAsync(sut);

        metrics.Longs.Should().NotContain(m => m.Instrument == "ordersphere.advisor.tokens");
    }

    /// <summary>
    /// Captures measurements emitted on the shared "OrderSphere" meter for the
    /// duration of a single test. Within this assembly only ObservabilityChatClient
    /// writes to that meter, and the class runs its tests sequentially, so each
    /// collector observes only the measurements produced by its own test.
    /// </summary>
    private sealed class MetricCollector : IDisposable
    {
        private readonly MeterListener _listener = new();

        public List<MetricSample<long>> Longs { get; } = [];
        public List<MetricSample<double>> Doubles { get; } = [];

        public MetricCollector()
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == ApplicationDiagnostics.MeterName)
                    listener.EnableMeasurementEvents(instrument);
            };
            _listener.SetMeasurementEventCallback<long>((inst, value, tags, _) =>
                Longs.Add(new MetricSample<long>(inst.Name, value, tags.ToArray())));
            _listener.SetMeasurementEventCallback<double>((inst, value, tags, _) =>
                Doubles.Add(new MetricSample<double>(inst.Name, value, tags.ToArray())));
            _listener.Start();
        }

        public void Dispose() => _listener.Dispose();
    }

    private readonly record struct MetricSample<T>(
        string Instrument, T Value, KeyValuePair<string, object?>[] Tags)
    {
        public string? Tag(string key) =>
            Tags.FirstOrDefault(t => t.Key == key).Value?.ToString();
    }
}
