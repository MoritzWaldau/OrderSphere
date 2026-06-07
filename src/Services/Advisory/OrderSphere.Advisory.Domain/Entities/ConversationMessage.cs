using OrderSphere.BuildingBlocks.Abstraction;

namespace OrderSphere.Advisory.Domain.Entities;

// A single turn in a conversation. Role is "user" or "assistant"; CreatedAt
// (from AuditableEntity) provides ordering.
public sealed class ConversationMessage : AuditableEntity<Guid>
{
    public Guid ConversationId { get; set; }

    public required string Role { get; set; }

    public required string Text { get; set; }
}
