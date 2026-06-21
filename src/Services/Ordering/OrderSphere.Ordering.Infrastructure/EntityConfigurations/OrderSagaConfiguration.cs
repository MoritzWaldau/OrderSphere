using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSphere.Ordering.Domain.Entities;

namespace OrderSphere.Ordering.Infrastructure.EntityConfigurations;

public sealed class OrderSagaConfiguration : IEntityTypeConfiguration<OrderSaga>
{
    public void Configure(EntityTypeBuilder<OrderSaga> builder)
    {
        builder.ToTable("order_sagas");

        // Correlation id is the natural key and is supplied explicitly (never generated).
        builder.HasKey(s => s.CorrelationId);
        builder.Property(s => s.CorrelationId).ValueGeneratedNever();

        // Stored as a readable string so the projection is inspectable in the database.
        builder.Property(s => s.State)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(s => s.LastError).HasMaxLength(1024);

        // Lookup by order id for cross-referencing from the order side.
        builder.HasIndex(s => s.OrderId);

        // No soft-delete query filter: this is a read-model, not an AuditableEntity.
    }
}
