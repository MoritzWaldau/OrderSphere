using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Domain.Entities;
using OrderSphere.Ordering.Domain.Enums;

namespace OrderSphere.Ordering.Infrastructure.EntityConfigurations;

public sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("coupons");
        builder.HasKey(c => c.Id);
        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.Property(c => c.Code).HasMaxLength(40).IsRequired();
        builder.HasIndex(c => c.Code).IsUnique();

        builder.Property(c => c.DiscountType).HasConversion<int>().IsRequired();
        builder.Property(c => c.Value).HasPrecision(18, 2).IsRequired();
        builder.Property(c => c.MinSubtotal).HasPrecision(18, 2);
        builder.Property(c => c.RedeemedCount).IsRequired();
        builder.Property(c => c.IsActive).IsRequired();

        // Seed the two codes the former hardcoded handler supported, so behavior is preserved.
        var seedCreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new
            {
                Id = CouponId.From(new Guid("0192a000-0000-7000-8000-000000000001")),
                Code = "WELCOME10",
                DiscountType = DiscountType.Flat,
                Value = 10m,
                MinSubtotal = (decimal?)null,
                ValidFrom = (DateTime?)null,
                ValidUntil = (DateTime?)null,
                MaxRedemptions = (int?)null,
                RedeemedCount = 0,
                IsActive = true,
                CreatedAt = seedCreatedAt,
                IsDeleted = false,
            },
            new
            {
                Id = CouponId.From(new Guid("0192a000-0000-7000-8000-000000000002")),
                Code = "SUMMER15",
                DiscountType = DiscountType.Flat,
                Value = 15m,
                MinSubtotal = (decimal?)100m,
                ValidFrom = (DateTime?)null,
                ValidUntil = (DateTime?)null,
                MaxRedemptions = (int?)null,
                RedeemedCount = 0,
                IsActive = true,
                CreatedAt = seedCreatedAt,
                IsDeleted = false,
            });
    }
}
