using OrderSphere.BuildingBlocks.Abstraction;

namespace OrderSphere.Advisory.Domain.Entities;

// A persisted advisory chat conversation, owned by a single customer. Continuity
// across requests, restarts, and instances is provided by SerializedSession — the
// agent's session state (chat history) serialized to JSON. Messages keeps a
// human-readable transcript for display and audit.
public sealed class Conversation : AuditableEntity<Guid>
{
    // Client-supplied conversation identifier (opaque, e.g. a 32-char hex string).
    public required string ConversationKey { get; set; }

    // Keycloak subject of the owning customer. Scopes conversations per user.
    public required string CustomerSub { get; set; }

    // Agent session state (chat history) serialized to JSON. Null until the first turn.
    public string? SerializedSession { get; set; }

    public List<ConversationMessage> Messages { get; set; } = [];
}
