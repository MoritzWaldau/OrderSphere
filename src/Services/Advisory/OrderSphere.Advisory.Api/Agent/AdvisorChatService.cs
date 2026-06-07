using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI.Chat;
using OrderSphere.Advisory.Application.Abstractions;
using OrderSphere.Advisory.Domain.Entities;

namespace OrderSphere.Advisory.Api.Agent;

// Drives a streaming customer-advisory conversation.
//
// Design note: the agent is constructed per request because its tools are the
// MCP tools of the *current user* — the MCP client carries the caller's bearer
// token so user-scoped tools (orders, profile) resolve the correct customer.
//
// Conversation continuity is durable: the agent session (chat history) is
// serialized to advisory-db after each turn and rehydrated on the next request,
// so context survives process restarts and is shared across instances. A
// human-readable transcript is stored alongside for display and audit.
//
// We use the Azure OpenAI chat client (an IChatClient) rather than a
// service-managed Foundry agent precisely because tools are bound per request;
// this is the "Any IChatClient" path of Microsoft Agent Framework.
public sealed class AdvisorChatService(
    IHttpContextAccessor httpContextAccessor,
    IAdvisoryDbContext db,
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

        var customerSub = ResolveCustomerSub();

        await using var mcpClient = await CreateMcpClientAsync(ct);
        var tools = await mcpClient.ListToolsAsync(cancellationToken: ct);

        ChatClient chatClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
            .GetChatClient(deployment);

        AIAgent agent = chatClient.AsAIAgent(
            instructions: SystemInstructions,
            name: "OrderSphereAdvisor",
            tools: [.. tools.Cast<AITool>()]);

        // Load (or start) the conversation. Persistence is per customer; without a
        // resolvable subject the conversation runs ephemerally for this request.
        Conversation? conversation = customerSub is null
            ? null
            : await db.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(
                    c => c.CustomerSub == customerSub && c.ConversationKey == conversationId, ct);

        AgentSession session = await RehydrateOrCreateSessionAsync(agent, conversation, ct);

        var assistant = new StringBuilder();
        await foreach (var update in agent.RunStreamingAsync(message, session, cancellationToken: ct))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                assistant.Append(update.Text);
                yield return update.Text;
            }
        }

        if (customerSub is not null)
        {
            await PersistTurnAsync(agent, session, conversation, customerSub, conversationId,
                message, assistant.ToString(), ct);
        }
    }

    private async Task<AgentSession> RehydrateOrCreateSessionAsync(
        AIAgent agent, Conversation? conversation, CancellationToken ct)
    {
        if (conversation?.SerializedSession is { Length: > 0 } serialized)
        {
            try
            {
                using var doc = JsonDocument.Parse(serialized);
                return await agent.DeserializeSessionAsync(
                    doc.RootElement, jsonSerializerOptions: null, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                // A stored session that no longer deserializes (e.g. agent shape changed)
                // must not break the chat — start fresh.
                logger.LogWarning(ex, "Could not rehydrate stored session; starting a new one.");
            }
        }

        return await agent.CreateSessionAsync(ct);
    }

    private async Task PersistTurnAsync(
        AIAgent agent,
        AgentSession session,
        Conversation? conversation,
        string customerSub,
        string conversationId,
        string userMessage,
        string assistantMessage,
        CancellationToken ct)
    {
        string serializedSession;
        try
        {
            var state = await agent.SerializeSessionAsync(
                session, jsonSerializerOptions: null, cancellationToken: ct);
            serializedSession = state.GetRawText();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not serialize session; transcript will be stored without state.");
            serializedSession = string.Empty;
        }

        if (conversation is null)
        {
            conversation = new Conversation
            {
                ConversationKey = conversationId,
                CustomerSub = customerSub,
                SerializedSession = serializedSession.Length > 0 ? serializedSession : null
            };
            db.Conversations.Add(conversation);
        }
        else if (serializedSession.Length > 0)
        {
            conversation.SerializedSession = serializedSession;
        }

        conversation.Messages.Add(new ConversationMessage
        {
            ConversationId = conversation.Id,
            Role = "user",
            Text = userMessage
        });

        if (assistantMessage.Length > 0)
        {
            conversation.Messages.Add(new ConversationMessage
            {
                ConversationId = conversation.Id,
                Role = "assistant",
                Text = assistantMessage
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private string? ResolveCustomerSub()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        // JwtBearer maps "sub" to NameIdentifier by default; accept either.
        var sub = user.FindFirst("sub")?.Value
                  ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrWhiteSpace(sub) ? null : sub;
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
