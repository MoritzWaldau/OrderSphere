using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSphere.Webhooks.Domain.Entities;

namespace OrderSphere.Webhooks.Infrastructure.EntityConfigurations;

internal sealed class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("webhook_subscriptions");

        builder.HasKey(s => s.Id);
        builder.HasQueryFilter(s => !s.IsDeleted);

        builder.HasIndex(s => s.CustomerId);
        builder.HasIndex(s => new { s.CustomerId, s.IsActive });

        builder.Property(s => s.Url).HasMaxLength(2048);
        builder.Property(s => s.Secret).HasMaxLength(256);
        builder.Property(s => s.Events).HasMaxLength(512);
    }
}
