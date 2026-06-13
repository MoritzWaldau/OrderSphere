using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSphere.Webhooks.Domain.Entities;

namespace OrderSphere.Webhooks.Infrastructure.EntityConfigurations;

internal sealed class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.ToTable("webhook_deliveries");

        builder.HasKey(d => d.Id);
        builder.HasQueryFilter(d => !d.IsDeleted);

        builder.HasIndex(d => d.SubscriptionId);
        builder.HasIndex(d => d.EventId);
        builder.HasIndex(d => new { d.Status, d.NextRetryAt });

        builder.Property(d => d.EventType).HasMaxLength(128);
        builder.Property(d => d.Payload).HasColumnType("text");
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.LastError).HasMaxLength(1024);
    }
}
