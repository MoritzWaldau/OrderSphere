using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSphere.Infrastructure.Outbox;

namespace OrderSphere.Infrastructure.EntityConfigurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Content).IsRequired();
        builder.HasIndex(x => x.ProcessedAt);
    }
}
