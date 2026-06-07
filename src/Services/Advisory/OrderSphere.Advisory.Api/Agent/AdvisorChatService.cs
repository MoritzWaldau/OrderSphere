using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI.Chat;

namespace OrderSphere.Advisory.Api.Agent;

// Drives a streaming customer-advisory conversation.
//
// Design note: the agent is constructed per request because its tools are the
// MCP tools of the *current user* — the MCP client carries the caller's bearer
// token so user-scoped tools (orders, profile) resolve the correct customer.
// The Azure OpenAI client is a singleton; conversation continuity is kept via an
// in-memory session cache keyed by conversation id.
//
// We use the Azure OpenAI chat client (an IChatClient) rather than a
// service-managed Foundry agent precisely because tools are bound per request;
// this is the "Any IChatClient" path of Microsoft Agent Framework.
public sealed class AdvisorChatService(
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration,
    ILogger<AdvisorChatService> logger)
{
    private const string SystemInstructions =
        """
        Du bist der Kundenberater von OrderSphere, einem Online-Shop.
        Antworte auf Deutsch, sachlich und freundlich. Hilf bei Produktsuche und
        -beratung, Bestell- und Lieferauskünften sowie Konto-/Profilfragen.

        Strikte Regeln:
        - Nutze ausschließlich die bereitgestellten Tools, um an Daten zu kommen.
          Erfinde niemals Produkte, Preise, Bestellungen oder Profildaten.
        - Wenn ein Tool keine Daten liefert (z. B. weil der Nutzer nicht angemeldet
          ist), sage das offen und biete an, was ohne Anmeldung möglich ist.
        - Gib keine internen IDs oder technischen Fehlermeldungen an den Kunden weiter!
        """;

    private static readonly ConcurrentDictionary<string, AgentSession> Sessions = new();

    public async IAsyncEnumerable<string> StreamAsync(
        string conversationId,
        string message,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var endpoint = configuration["Foundry:Endpoint"];
        var deployment = configuration["Foundry:Deployment"] ?? "gpt-4o-mini";

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            logger.LogWarning("Foundry:Endpoint is not configured; advisory agent is unavailable.");
            yield return "Der Berater ist derzeit nicht konfiguriert. Bitte versuche es später erneut.";
            yield break;
        }

        await using var mcpClient = await CreateMcpClientAsync(ct);
        var tools = await mcpClient.ListToolsAsync(cancellationToken: ct);

        ChatClient chatClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
            .GetChatClient(deployment);

        AIAgent agent = chatClient.AsAIAgent(
            instructions: SystemInstructions,
            name: "OrderSphereAdvisor",
            tools: [.. tools.Cast<AITool>()]);

        if (!Sessions.TryGetValue(conversationId, out var session))
        {
            session = await agent.CreateSessionAsync(ct);
            Sessions.TryAdd(conversationId, session);
        }

        await foreach (var update in agent.RunStreamingAsync(message, session, cancellationToken: ct))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                yield return update.Text;
            }
        }
    }

    private async Task<McpClient> CreateMcpClientAsync(CancellationToken ct)
    {
        var mcpBaseUrl = configuration["Services:Mcp:BaseUrl"] ?? "http://ordersphere-mcp";
        var headers = new Dictionary<string, string>();

        var authorization = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization))
        {
            headers["Authorization"] = authorization;
        }

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri($"{mcpBaseUrl.TrimEnd('/')}/mcp"),
            AdditionalHeaders = headers
        });

        return await McpClient.CreateAsync(transport, cancellationToken: ct);
    }
}
