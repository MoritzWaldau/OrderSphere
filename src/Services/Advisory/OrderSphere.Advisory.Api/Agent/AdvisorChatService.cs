using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OrderSphere.Advisory.Application.Abstractions;
using OrderSphere.Advisory.Domain.Entities;

namespace OrderSphere.Advisory.Api.Agent;

// Drives a streaming customer-advisory conversation.
//
// Design note: the agent is constructed per request because its tools are the
// MCP tools of the *current user* — the MCP client carries the caller's bearer
// token so user-scoped tools (orders, profile) resolve the correct customer.
// The underlying chat-client pipeline (credential, reducer, telemetry) is shared
// via IAdvisorChatClientFactory.
//
// Conversation continuity is durable: the agent session (chat history) is
// serialized to advisory-db after each turn and rehydrated on the next request,
// so context survives process restarts and is shared across instances. A
// human-readable transcript is stored alongside for display and audit.
public sealed class AdvisorChatService(
    IHttpContextAccessor httpContextAccessor,
    IAdvisoryDbContext db,
    IAdvisorChatClientFactory chatClientFactory,
    IAdvisorToolSource toolSource,
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

    private const string NotConfiguredMessage =
        "Der Berater ist derzeit nicht konfiguriert. Bitte versuche es später erneut.";

    private const string TurnFailedMessage =
        "Entschuldigung, gerade ist etwas schiefgelaufen. Bitte versuche es gleich noch einmal.";

    public async IAsyncEnumerable<AdvisorStreamEvent> StreamAsync(
        string conversationId,
        string message,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var chatClient = chatClientFactory.GetChatClient();
        if (chatClient is null)
        {
            logger.LogWarning("Foundry:Endpoint is not configured; advisory agent is unavailable.");
            yield return AdvisorStreamEvent.OfText(NotConfiguredMessage);
            yield break;
        }

        var customerSub = ResolveCustomerSub();

        AdvisorToolLease? toolLease = null;
        try
        {
            // Setup (MCP connection, tool discovery) can fail without breaking the
            // SSE contract: surface a friendly message instead of a broken stream.
            AIAgent? agent = null;
            try
            {
                toolLease = await toolSource.AcquireAsync(ct);
                agent = chatClient.AsAIAgent(
                    instructions: SystemInstructions,
                    name: "OrderSphereAdvisor",
                    tools: [.. toolLease.Tools]);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Advisory agent setup failed (MCP connection or tool discovery).");
            }

            if (ct.IsCancellationRequested)
            {
                yield break;
            }

            if (agent is null)
            {
                yield return AdvisorStreamEvent.OfText(TurnFailedMessage);
                yield break;
            }

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
            var failed = false;
            string? lastToolLabel = null;

            // yield is illegal inside a catch block, so the stream is consumed via the
            // enumerator with MoveNextAsync guarded individually.
            await using var updates = agent
                .RunStreamingAsync(message, session, cancellationToken: ct)
                .GetAsyncEnumerator(ct);

            while (true)
            {
                bool moved;
                try
                {
                    moved = await updates.MoveNextAsync();
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    yield break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Advisory agent stream failed mid-turn.");
                    failed = true;
                    moved = false;
                }

                if (!moved)
                {
                    break;
                }

                var update = updates.Current;

                foreach (var call in update.Contents.OfType<FunctionCallContent>())
                {
                    var label = ToolLabel(call.Name);
                    if (label != lastToolLabel)
                    {
                        lastToolLabel = label;
                        yield return AdvisorStreamEvent.OfTool(label);
                    }
                }

                if (!string.IsNullOrEmpty(update.Text))
                {
                    assistant.Append(update.Text);
                    yield return AdvisorStreamEvent.OfText(update.Text);
                }
            }

            if (failed)
            {
                // The turn did not complete; do not persist a half-finished exchange.
                yield return AdvisorStreamEvent.OfText(TurnFailedMessage);
                yield break;
            }

            if (customerSub is not null)
            {
                await PersistTurnAsync(agent, session, conversation, customerSub, conversationId,
                    message, assistant.ToString(), ct);
            }
        }
        finally
        {
            if (toolLease is not null)
            {
                await toolLease.DisposeAsync();
            }
        }
    }

    // User-facing German labels for tool activity shown in the chat UI.
    private static string ToolLabel(string toolName) => toolName switch
    {
        "search_products" => "Durchsuche den Katalog",
        "get_product" => "Rufe Produktdetails ab",
        "list_categories" => "Lade Kategorien",
        "get_my_orders" => "Rufe deine Bestellungen ab",
        "get_order_status" => "Prüfe den Bestellstatus",
        "validate_coupon" => "Prüfe den Gutschein",
        "get_my_cart" => "Schaue in deinen Warenkorb",
        "get_payment_status" => "Prüfe den Zahlungsstatus",
        "get_my_profile" => "Rufe dein Profil ab",
        "list_my_addresses" => "Rufe deine Adressen ab",
        _ => "Rufe Informationen ab"
    };

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
        => CustomerContext.ResolveSub(httpContextAccessor.HttpContext?.User);
}
