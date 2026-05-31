using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSphere.Ordering.Domain.Entities;

namespace OrderSphere.Ordering.Infrastructure.EntityConfigurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductId).IsRequired();
        builder.Property(i => i.ProductName).IsRequired().HasMaxLength(256);
        builder.Property(i => i.Quantity).IsRequired();   // Quantity → int via QuantityConverter global convention

        builder.ComplexProperty(i => i.Price, b =>
        {
            b.Property(m => m.Amount)
             .HasColumnName("price")
             .HasPrecision(18, 2)
             .IsRequired();
            b.Property(m => m.Currency)
             .HasColumnName("price_currency")
             .HasMaxLength(3)
             .IsRequired()
             .HasDefaultValue("EUR");
        });

        builder.HasOne<Order>()
            .WithMany(o => o.Items)
            .HasForeignKey("OrderId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
