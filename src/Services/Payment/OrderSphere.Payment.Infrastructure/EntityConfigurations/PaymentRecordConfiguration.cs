using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSphere.Payment.Domain.Entities;

namespace OrderSphere.Payment.Infrastructure.EntityConfigurations;

internal sealed class PaymentRecordConfiguration : IEntityTypeConfiguration<PaymentRecord>
{
    public void Configure(EntityTypeBuilder<PaymentRecord> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id);
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => p.OrderId).IsUnique();
        builder.HasIndex(p => p.CorrelationId);

        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Currency).HasMaxLength(3);
        builder.Property(p => p.PaymentMethod).HasMaxLength(50);
        builder.Property(p => p.CustomerEmail).HasMaxLength(256);
        builder.Property(p => p.TransactionId).HasMaxLength(256);
        builder.Property(p => p.FailureReason).HasMaxLength(1024);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
    }
}
