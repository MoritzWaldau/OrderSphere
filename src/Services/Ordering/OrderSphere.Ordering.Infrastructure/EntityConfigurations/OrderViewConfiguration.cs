using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSphere.Ordering.Domain.ReadModels;

namespace OrderSphere.Ordering.Infrastructure.EntityConfigurations;

/// <summary>
/// Maps the <see cref="OrderView"/> read projection to the order tables. The schema is identical
/// to the pre-event-sourcing aggregate mapping — the read side is unchanged, only its writer is.
/// </summary>
public sealed class OrderViewConfiguration : IEntityTypeConfiguration<OrderView>
{
    public void Configure(EntityTypeBuilder<OrderView> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);
        builder.HasQueryFilter(o => !o.IsDeleted);

        builder.Property(o => o.CustomerId).IsRequired();
        builder.Property(o => o.PaymentMethod).HasConversion<int>().IsRequired();
        builder.Property(o => o.Status).HasConversion<int>().IsRequired();
        builder.Property(o => o.TrackingNumber).HasMaxLength(20);
        builder.Property(o => o.CorrelationId).IsRequired();
        builder.Property(o => o.CouponCode).HasMaxLength(40);
        builder.Property(o => o.DiscountAmount).HasPrecision(18, 2);
        builder.Property(o => o.ShippingCost).HasPrecision(18, 2);

        builder.HasIndex(o => o.CorrelationId).IsUnique();

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey("OrderId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Append-only status timeline, owned by the order (auto-loaded with the aggregate).
        builder.OwnsMany(o => o.StatusHistory, history =>
        {
            history.ToTable("order_status_history");
            history.HasKey(h => h.Id);
            history.WithOwner().HasForeignKey("OrderId");
            history.Property(h => h.Id).ValueGeneratedNever();
            history.Property(h => h.Status).HasConversion<int>().IsRequired();
            history.Property(h => h.OccurredAt).IsRequired();
            history.Property(h => h.Note).HasMaxLength(200);
        });

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
