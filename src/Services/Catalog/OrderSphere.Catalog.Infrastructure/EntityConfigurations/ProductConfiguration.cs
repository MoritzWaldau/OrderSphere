namespace OrderSphere.Catalog.Infrastructure.EntityConfigurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.ComplexProperty(p => p.Price, b =>
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
        builder.Property(p => p.Stock).IsRequired();
        builder.Property(p => p.SKU).IsRequired().HasMaxLength(100);
        builder.HasIndex(p => p.SKU).IsUnique().HasDatabaseName("IX_products_sku");
        builder.Property(p => p.ImageUrl).HasMaxLength(500);
        builder.Property(p => p.ImageBlobName).HasMaxLength(500);
        builder.Property(p => p.CategoryId).IsRequired();
        builder.Property(p => p.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(p => p.AverageRating).IsRequired().HasDefaultValue(0d);
        builder.Property(p => p.ReviewCount).IsRequired().HasDefaultValue(0);
        builder.HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
