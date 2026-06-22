using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSphere.Ordering.Domain.Entities;

namespace OrderSphere.Ordering.Infrastructure.EntityConfigurations;

public sealed class OrderHistoryEntryConfiguration : IEntityTypeConfiguration<OrderHistoryEntry>
{
    public void Configure(EntityTypeBuilder<OrderHistoryEntry> builder)
    {
        builder.ToTable("order_history");

        // Client-generated v7 key; never store-generated.
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.OrderId).IsRequired();
        builder.Property(e => e.CorrelationId).IsRequired();

        builder.Property(e => e.CustomerEmail).HasMaxLength(256).IsRequired();
        builder.Property(e => e.PreviousStatus).HasMaxLength(32).IsRequired();
        builder.Property(e => e.NewStatus).HasMaxLength(32).IsRequired();
        builder.Property(e => e.OccurredAt).IsRequired();

        // Per-order timeline (oldest-first) reads.
        builder.HasIndex(e => new { e.OrderId, e.OccurredAt });

        // Global activity feed is ordered newest-first across all orders.
        builder.HasIndex(e => e.OccurredAt);

        // Cross-order lookups by customer for the denormalised feed.
        builder.HasIndex(e => e.CustomerEmail);

        // No soft-delete query filter: this is a read-model, not an AuditableEntity.
    }
}
