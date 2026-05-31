using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSphere.BuildingBlocks.EventBus.Inbox;

namespace OrderSphere.Payment.Infrastructure.EntityConfigurations;

internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");

        builder.HasKey(m => m.EventId);

        builder.Property(m => m.EventType).HasMaxLength(256);

        builder.HasIndex(m => m.ProcessedAt);
    }
}
