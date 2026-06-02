using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSphere.Ordering.Infrastructure.Outbox;

namespace OrderSphere.Ordering.Infrastructure.EntityConfigurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.RetryCount).HasDefaultValue(0);
        // Composite index optimises the dispatcher query: WHERE ProcessedAt IS NULL ORDER BY OccurredAt
        builder.HasIndex(x => new { x.ProcessedAt, x.OccurredAt });
        builder.HasIndex(x => x.RetryCount);
    }
}
