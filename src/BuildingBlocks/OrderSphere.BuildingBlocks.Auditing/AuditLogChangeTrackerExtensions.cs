using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Security;

namespace OrderSphere.BuildingBlocks.Auditing;

public static class AuditLogChangeTrackerExtensions
{
    /// <summary>
    /// Stages one <see cref="AuditLogEntry"/> per added/modified/deleted <see cref="IAuditableEntity"/>.
    /// Must run before <c>base.SaveChangesAsync</c> — original property values are only available
    /// on the tracker up to that point, since EF clears them once the save completes.
    /// </summary>
    public static void CaptureAuditLog(this ChangeTracker changeTracker, ICurrentUser currentUser)
    {
        var changedBy = currentUser.Sub ?? currentUser.Email ?? "system";

        var trackedEntries = changeTracker.Entries()
            .Where(e => e.Entity is IAuditableEntity && e.State
                is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        var auditLogEntries = new List<AuditLogEntry>();

        foreach (var entry in trackedEntries)
        {
            var changes = BuildChanges(entry);
            if (entry.State == EntityState.Modified && changes.Count == 0)
                continue;

            var entityId = entry.Properties
                .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "";

            auditLogEntries.Add(new AuditLogEntry
            {
                EntityType = entry.Entity.GetType().Name,
                EntityId = entityId,
                Action = entry.State switch
                {
                    EntityState.Added => AuditAction.Created,
                    EntityState.Deleted => AuditAction.Deleted,
                    _ => AuditAction.Modified,
                },
                ChangedBy = changedBy,
                Changes = JsonSerializer.Serialize(changes),
            });
        }

        if (auditLogEntries.Count > 0)
            changeTracker.Context.Set<AuditLogEntry>().AddRange(auditLogEntries);
    }

    private static Dictionary<string, object?> BuildChanges(EntityEntry entry)
    {
        var changes = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsPrimaryKey())
                continue;

            switch (entry.State)
            {
                case EntityState.Added:
                case EntityState.Deleted:
                    changes[property.Metadata.Name] = property.CurrentValue;
                    break;
                case EntityState.Modified when property.IsModified:
                    changes[property.Metadata.Name] = new { Old = property.OriginalValue, New = property.CurrentValue };
                    break;
            }
        }

        return changes;
    }
}
