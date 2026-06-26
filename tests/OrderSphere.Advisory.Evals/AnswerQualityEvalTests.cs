using Azure.AI.OpenAI;
using Azure.Identity;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using OrderSphere.Advisory.Api.Agent;
using Xunit;

namespace OrderSphere.Advisory.Evals;

/// <summary>
/// LLM-as-Judge quality evaluations. Require a live Foundry endpoint;
/// skipped automatically in CI when <c>Foundry__Endpoint</c> is not set.
/// Run locally or in a dedicated eval CI job with the environment variable set.
/// </summary>
public sealed class AnswerQualityEvalTests
{
    // Reads Foundry:Endpoint from env var (ASP.NET Core convention: colon → double underscore).
    private static string? FoundryEndpoint =>
        Environment.GetEnvironmentVariable("Foundry__Endpoint");

    private static string FoundryDeployment =>
        Environment.GetEnvironmentVariable("Foundry__Deployment") ?? "gpt-4o-mini";

    private static IChatClient BuildLiveClient() =>
        new AzureOpenAIClient(new Uri(FoundryEndpoint!), new DefaultAzureCredential())
            .GetChatClient(FoundryDeployment)
            .AsIChatClient();

    public static IEnumerable<object[]> QualityCases =>
    [
        [
            "Hallo, was kann ich hier kaufen?",
            "Der Agent erklärt die Produktpalette des Shops."
        ],
        [
            "Ich suche Sneaker für unter 100 Euro.",
            "Der Agent bietet an, die Produktsuche zu starten oder nennt relevante Schritte."
        ],
        [
            "Wie kann ich eine Bestellung verfolgen?",
            "Der Agent erklärt, wie man den Bestellstatus einsehen kann."
        ],
    ];

    [Theory]
    [MemberData(nameof(QualityCases))]
    public async Task AgentAnswer_MeetsCoherenceThreshold(string question, string criteria)
    {
        if (string.IsNullOrWhiteSpace(FoundryEndpoint))
            return; // Foundry__Endpoint not set — skipping live eval (CI non-blocking).

        var answerClient = BuildLiveClient();
        var judgeClient = BuildLiveClient();
        var svc = EvalServiceFactory.Build(answerClient);

        var events = new List<AdvisorStreamEvent>();
        await foreach (var e in svc.StreamAsync("eval-quality", question))
            events.Add(e);

        var answer = string.Concat(
            events.Where(e => e.Kind == AdvisorStreamEventKind.Text).Select(e => e.Text));

        answer.Should().NotBeNullOrWhiteSpace(because: "agent should produce a response");

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, question),
        };

        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, answer)]);

        var judgeConfig = new ChatConfiguration(judgeClient);

        var coherenceResult = await new CoherenceEvaluator()
            .EvaluateAsync(messages, response, judgeConfig);

        var relevanceResult = await new RelevanceEvaluator()
            .EvaluateAsync(messages, response, judgeConfig);

        coherenceResult.TryGet<NumericMetric>(CoherenceEvaluator.CoherenceMetricName, out var coherence).Should().BeTrue();
        relevanceResult.TryGet<NumericMetric>(RelevanceEvaluator.RelevanceMetricName, out var relevance).Should().BeTrue();

        // MEAI quality scores are 1–5; scores rated Unacceptable indicate the answer failed.
        coherence!.Interpretation?.Rating.Should().NotBe(EvaluationRating.Unacceptable,
            because: $"answer should be coherent — criteria: {criteria}");
        relevance!.Interpretation?.Rating.Should().NotBe(EvaluationRating.Unacceptable,
            because: $"answer should be relevant — criteria: {criteria}");
    }
}
