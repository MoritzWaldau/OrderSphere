using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.BuildingBlocks.Auditing;

/// <summary>
/// Generic <see cref="IAuditLogQuery"/> backed by any service's <see cref="DbContext"/> that has
/// applied <see cref="AuditLogEntryConfiguration"/>. Register once per service:
/// <c>services.AddScoped&lt;IAuditLogQuery, EfAuditLogQuery&lt;OrderingDbContext&gt;&gt;();</c>
/// </summary>
public sealed class EfAuditLogQuery<TContext>(TContext context) : IAuditLogQuery
    where TContext : DbContext
{
    public async Task<Result<IReadOnlyList<AuditLogEntryDto>>> QueryAsync(
        string? entityType, string? entityId, CancellationToken cancellationToken)
    {
        var query = context.Set<AuditLogEntry>().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(e => e.EntityType == entityType);

        if (!string.IsNullOrWhiteSpace(entityId))
            query = query.Where(e => e.EntityId == entityId);

        var entries = await query
            .OrderByDescending(e => e.OccurredAt)
            .Take(200)
            .Select(e => new AuditLogEntryDto(
                e.Id, e.EntityType, e.EntityId, e.Action.ToString(), e.ChangedBy, e.OccurredAt, e.Changes))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<AuditLogEntryDto>>.Success(entries);
    }
}
