using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OrderSphere.BuildingBlocks.Auditing;

public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("audit_log_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityType).IsRequired().HasMaxLength(256);
        builder.Property(x => x.EntityId).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Action).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ChangedBy).HasMaxLength(256);
        builder.Property(x => x.Changes).IsRequired();
        // Supports the admin query pattern: WHERE EntityType = ? AND EntityId = ? ORDER BY OccurredAt.
        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredAt });
    }
}
