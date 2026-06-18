namespace OrderSphere.Catalog.Infrastructure.EntityConfigurations;

public sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("stock_reservations");
        builder.HasKey(r => r.Id);
        builder.HasQueryFilter(r => !r.IsDeleted);

        builder.Property(r => r.CorrelationId).IsRequired();
        builder.Property(r => r.ProductId).IsRequired();
        builder.Property(r => r.Quantity).IsRequired();
        builder.Property(r => r.Status).IsRequired().HasConversion<int>();
        builder.Property(r => r.ExpiresAt).IsRequired();

        // Confirm/release operate by correlation id; availability sums by product + status.
        builder.HasIndex(r => r.CorrelationId).HasDatabaseName("IX_stock_reservations_correlation");
        builder.HasIndex(r => new { r.ProductId, r.Status }).HasDatabaseName("IX_stock_reservations_product_status");
    }
}
