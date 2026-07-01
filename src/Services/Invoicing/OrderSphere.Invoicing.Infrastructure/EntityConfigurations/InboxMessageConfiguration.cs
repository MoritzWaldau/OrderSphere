using OrderSphere.BuildingBlocks.EventBus.Inbox;

namespace OrderSphere.Invoicing.Infrastructure.EntityConfigurations;

internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
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
