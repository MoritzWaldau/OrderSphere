using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.BuildingBlocks.Auditing;

public sealed record AuditLogEntryDto(
    Guid Id,
    string EntityType,
    string EntityId,
    string Action,
    string? ChangedBy,
    DateTime OccurredAt,
    string Changes);

public interface IAuditLogQuery
{
    Task<Result<IReadOnlyList<AuditLogEntryDto>>> QueryAsync(
        string? entityType, string? entityId, CancellationToken cancellationToken);
}
