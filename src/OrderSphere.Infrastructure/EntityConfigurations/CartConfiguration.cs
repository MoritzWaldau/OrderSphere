using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSphere.Domain.Entities;

namespace OrderSphere.Infrastructure.EntityConfigurations;

public sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("carts");

        builder.HasKey(x => x.Id);

        //builder.HasIndex(x => x.CustomerId)
        //    .IsUnique();

        builder.Property(x => x.CustomerId)
            .IsRequired();

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey("CartId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
