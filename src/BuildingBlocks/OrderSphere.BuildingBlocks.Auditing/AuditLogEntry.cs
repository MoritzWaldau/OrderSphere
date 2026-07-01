namespace OrderSphere.BuildingBlocks.Auditing;

/// <summary>
/// One row per tracked change to an <see cref="Abstraction.IAuditableEntity"/>. Not itself an
/// <c>AuditableEntity</c> — it is an immutable log record, not a domain entity that gets audited.
/// </summary>
public sealed class AuditLogEntry
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string EntityType { get; init; }
    public required string EntityId { get; init; }
    public required AuditAction Action { get; init; }
    public string? ChangedBy { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>JSON diff of changed properties (old/new for modifications, values for creations).</summary>
    public required string Changes { get; init; }
}

public enum AuditAction
{
    Created,
    Modified,
    Deleted,
}
