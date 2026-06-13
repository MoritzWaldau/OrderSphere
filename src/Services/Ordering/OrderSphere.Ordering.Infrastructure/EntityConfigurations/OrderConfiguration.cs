using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSphere.Ordering.Domain.Entities;

namespace OrderSphere.Ordering.Infrastructure.EntityConfigurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);
        builder.HasQueryFilter(o => !o.IsDeleted);

        builder.Property(o => o.CustomerId).IsRequired();
        builder.Property(o => o.PaymentMethod).HasConversion<int>().IsRequired();
        builder.Property(o => o.Status).HasConversion<int>().IsRequired();
        builder.Property(o => o.TrackingNumber).HasMaxLength(20);
        builder.Property(o => o.CorrelationId).IsRequired();

        builder.HasIndex(o => o.CorrelationId).IsUnique();

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey("OrderId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(o => o.ShippingAddress, address =>
        {
            address.Property(a => a.FirstName).HasColumnName("shipping_first_name");
            address.Property(a => a.LastName).HasColumnName("shipping_last_name");
            address.Property(a => a.Street).HasColumnName("shipping_street");
            address.Property(a => a.City).HasColumnName("shipping_city");
            address.Property(a => a.PostalCode).HasColumnName("shipping_postal_code");
            address.Property(a => a.Country).HasColumnName("shipping_country");
        });
    }
}
