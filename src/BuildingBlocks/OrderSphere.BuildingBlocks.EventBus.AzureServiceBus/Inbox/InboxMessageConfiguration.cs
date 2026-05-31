using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSphere.BuildingBlocks.EventBus.Inbox;

namespace OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Inbox;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");
        builder.HasKey(m => m.EventId);
        builder.Property(m => m.EventType).HasMaxLength(256).IsRequired();
        builder.Property(m => m.ProcessedAt).IsRequired();
        builder.HasIndex(m => m.ProcessedAt);
    }
}
