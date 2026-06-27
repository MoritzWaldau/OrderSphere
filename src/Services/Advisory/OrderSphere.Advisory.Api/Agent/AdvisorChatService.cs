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
        - Erwähne nur Produkte, die du tatsächlich über ein Tool abgerufen hast.
          Das System zeigt dem Nutzer automatisch Links zu den abgerufenen Produkten —
          du musst URLs oder Slugs nicht selbst ausgeben.

        Warenkorb-Aktion (add_to_cart):
        - Wenn der Nutzer ein Produkt in den Warenkorb legen möchte, rufe add_to_cart
          mit confirmed=false auf. Das Tool gibt ein JSON-Objekt mit dem Feld "__confirm__" zurück.
          Gib dieses JSON-Objekt WORTGENAU als deine einzige Antwort aus — kein weiterer Text.
        - Wenn der Nutzer bestätigt hat (Nachricht beginnt mit "Bestätigt:"), rufe add_to_cart
          mit denselben Argumenten und confirmed=true auf und teile das Ergebnis mit.
        - Wenn der Nutzer abbricht (Nachricht enthält "Abbrechen"), führe keine Aktion durch
          und bestätige die Ablehnung kurz.
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
            var toolCallNames = new Dictionary<string, string>(); // callId → tool name
            var citations = new List<ProductCitation>();

            // Text chunks are buffered until we can determine whether the model is
            // emitting a raw confirmation_required JSON payload (starts with {"__confirm__")
            // or normal prose. Once 14 chars are accumulated (enough to check the prefix),
            // normal chunks are flushed immediately; confirmation chunks stay buffered.
            var textBuffer = new List<string>();
            bool? isConfirmPayload = null;

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
                    toolCallNames[call.CallId] = call.Name;
                    var label = ToolLabel(call.Name);
                    if (label != lastToolLabel)
                    {
                        lastToolLabel = label;
                        yield return AdvisorStreamEvent.OfTool(label);
                    }
                }

                foreach (var result in update.Contents.OfType<FunctionResultContent>())
                {
                    if (toolCallNames.TryGetValue(result.CallId, out var toolName))
                        citations.AddRange(ExtractProductCitations(toolName, result.Result?.ToString()));
                }

                if (!string.IsNullOrEmpty(update.Text))
                {
                    assistant.Append(update.Text);

                    if (isConfirmPayload is null)
                    {
                        textBuffer.Add(update.Text);
                        var accumulated = string.Concat(textBuffer).TrimStart();
                        if (accumulated.Length >= 14)
                        {
                            isConfirmPayload = accumulated.StartsWith("{\"__confirm__\"", StringComparison.Ordinal);
                            if (isConfirmPayload == false)
                            {
                                foreach (var chunk in textBuffer)
                                    yield return AdvisorStreamEvent.OfText(chunk);
                                textBuffer.Clear();
                            }
                        }
                    }
                    else if (isConfirmPayload == false)
                    {
                        yield return AdvisorStreamEvent.OfText(update.Text);
                    }
                    // isConfirmPayload == true: keep buffering until stream ends
                }
            }

            if (failed)
            {
                // The turn did not complete; do not persist a half-finished exchange.
                yield return AdvisorStreamEvent.OfText(TurnFailedMessage);
                yield break;
            }

            // Flush anything buffered but never reaching the detection threshold (very
            // short responses), or emit the single confirmation event.
            if (isConfirmPayload == true)
            {
                yield return AdvisorStreamEvent.OfConfirm(string.Concat(textBuffer));
            }
            else if (textBuffer.Count > 0)
            {
                foreach (var chunk in textBuffer)
                    yield return AdvisorStreamEvent.OfText(chunk);
            }

            // Emit deduplicated product citations for this turn (after all text tokens).
            var distinctCitations = citations
                .DistinctBy(c => c.Slug)
                .Where(c => c.Slug.Length > 0)
                .ToList();
            if (distinctCitations.Count > 0)
                yield return AdvisorStreamEvent.OfCitation(
                    JsonSerializer.Serialize(distinctCitations, CamelCaseJson));

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

    private static readonly JsonSerializerOptions CamelCaseJson = new(JsonSerializerDefaults.Web);

    // User-facing German labels for tool activity shown in the chat UI.
    private static string ToolLabel(string toolName) => toolName switch
    {
        "search_products" => "Durchsuche den Katalog",
        "get_product" => "Rufe Produktdetails ab",
        "get_similar_products" => "Suche ähnliche Produkte",
        "list_categories" => "Lade Kategorien",
        "get_my_orders" => "Rufe deine Bestellungen ab",
        "get_order_status" => "Prüfe den Bestellstatus",
        "validate_coupon" => "Prüfe den Gutschein",
        "get_my_cart" => "Schaue in deinen Warenkorb",
        "get_payment_status" => "Prüfe den Zahlungsstatus",
        "get_my_profile" => "Rufe dein Profil ab",
        "list_my_addresses" => "Rufe deine Adressen ab",
        "add_to_cart" => "Lege in den Warenkorb",
        _ => "Rufe Informationen ab"
    };

    private static IReadOnlyList<ProductCitation> ExtractProductCitations(string toolName, string? rawJson)
    {
        if (rawJson is null) return [];
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;
            return toolName switch
            {
                "search_products" when root.TryGetProperty("products", out var products)
                    => CollectCitations(products.EnumerateArray()),
                "get_similar_products" when root.TryGetProperty("results", out var results)
                    => CollectCitations(results.EnumerateArray()),
                "get_product" when root.ValueKind == JsonValueKind.Object
                    => TryCitation(root) is { } c ? [c] : [],
                _ => []
            };
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<ProductCitation> CollectCitations(
        JsonElement.ArrayEnumerator items)
    {
        var result = new List<ProductCitation>();
        foreach (var item in items)
            if (TryCitation(item) is { } c)
                result.Add(c);
        return result;
    }

    private static ProductCitation? TryCitation(JsonElement el)
    {
        var slug = el.TryGetProperty("slug", out var s) ? s.GetString() : null;
        var name = el.TryGetProperty("name", out var n) ? n.GetString() : null;
        return !string.IsNullOrEmpty(slug) && !string.IsNullOrEmpty(name)
            ? new ProductCitation(slug, name)
            : null;
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
        => CustomerContext.ResolveSub(httpContextAccessor.HttpContext?.User);
}

/// <summary>A product referenced by a tool result in an advisory turn.</summary>
public sealed record ProductCitation(string Slug, string Name);
