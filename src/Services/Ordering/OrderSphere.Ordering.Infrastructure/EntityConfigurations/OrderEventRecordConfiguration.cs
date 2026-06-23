using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSphere.Ordering.Infrastructure.EventSourcing;

namespace OrderSphere.Ordering.Infrastructure.EntityConfigurations;

public sealed class OrderEventRecordConfiguration : IEntityTypeConfiguration<OrderEventRecord>
{
    public void Configure(EntityTypeBuilder<OrderEventRecord> builder)
    {
        builder.ToTable("order_events");

        // Composite key (stream, version) enforces a gap-free per-stream sequence and gives
        // optimistic concurrency: a duplicate (stream, version) insert fails the transaction.
        builder.HasKey(e => new { e.StreamId, e.Version });

        builder.Property(e => e.EventType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Payload).IsRequired();
        builder.Property(e => e.OccurredAt).IsRequired();

        // Replaying a stream reads by StreamId ordered by Version — already served by the PK.
        // A secondary index on OccurredAt supports chronological diagnostics over the log.
        builder.HasIndex(e => e.OccurredAt);
    }
}
