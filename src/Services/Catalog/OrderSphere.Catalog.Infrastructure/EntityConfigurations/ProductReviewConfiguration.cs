namespace OrderSphere.Catalog.Infrastructure.EntityConfigurations;

public sealed class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.ToTable("product_reviews");
        builder.HasKey(r => r.Id);
        builder.HasQueryFilter(r => !r.IsDeleted);

        builder.Property(r => r.ProductId).IsRequired();
        builder.Property(r => r.CustomerId).IsRequired();
        builder.Property(r => r.Rating).IsRequired();
        builder.Property(r => r.Title).IsRequired().HasMaxLength(150);
        builder.Property(r => r.Body).IsRequired().HasMaxLength(2000);
        builder.Property(r => r.IsVerifiedPurchase).IsRequired();
        builder.Property(r => r.Status).IsRequired().HasConversion<int>();

        // One review per customer per product.
        builder.HasIndex(r => new { r.ProductId, r.CustomerId })
            .IsUnique()
            .HasDatabaseName("IX_product_reviews_product_customer");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
