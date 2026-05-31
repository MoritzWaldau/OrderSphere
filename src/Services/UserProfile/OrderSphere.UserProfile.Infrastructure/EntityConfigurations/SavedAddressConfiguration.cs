using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSphere.UserProfile.Domain.Entities;

namespace OrderSphere.UserProfile.Infrastructure.EntityConfigurations;

public sealed class SavedAddressConfiguration : IEntityTypeConfiguration<SavedAddress>
{
    public void Configure(EntityTypeBuilder<SavedAddress> builder)
    {
        builder.ToTable("SavedAddresses");

        builder.HasKey(a => a.Id);

        builder.HasQueryFilter(a => !a.IsDeleted);

        builder.Property(a => a.Label).IsRequired().HasMaxLength(100);
        builder.Property(a => a.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(a => a.LastName).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Street).IsRequired().HasMaxLength(300);
        builder.Property(a => a.City).IsRequired().HasMaxLength(100);
        builder.Property(a => a.PostalCode).IsRequired().HasMaxLength(20);
        builder.Property(a => a.Country).IsRequired().HasMaxLength(100);
    }
}
