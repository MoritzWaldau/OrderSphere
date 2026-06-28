using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSphere.Ordering.Domain.Entities;

namespace OrderSphere.Ordering.Infrastructure.EntityConfigurations;

public sealed class ReturnRequestConfiguration : IEntityTypeConfiguration<ReturnRequest>
{
    public void Configure(EntityTypeBuilder<ReturnRequest> builder)
    {
        builder.ToTable("return_requests");
        builder.HasKey(r => r.Id);
        builder.HasQueryFilter(r => !r.IsDeleted);

        builder.Property(r => r.Status).HasConversion<int>().IsRequired();
        builder.Property(r => r.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(r => r.Resolution).HasMaxLength(1000);
        builder.Property(r => r.Currency).HasMaxLength(3).IsRequired();
        builder.Property(r => r.RequestedAt).IsRequired();

        builder.HasIndex(r => r.OrderId);
        builder.HasIndex(r => r.CustomerId);

        builder.OwnsMany(r => r.Items, items =>
        {
            items.ToTable("return_request_items");
            items.WithOwner().HasForeignKey("ReturnRequestId");
            items.HasKey(i => i.Id);
            items.Property(i => i.Id).ValueGeneratedNever();
            items.Property(i => i.ProductName).HasMaxLength(200).IsRequired();
            items.Property(i => i.Quantity).IsRequired();
            items.Property(i => i.UnitPrice).HasPrecision(18, 2).IsRequired();
        });
    }
}
