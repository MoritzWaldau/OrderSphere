using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSphere.UserProfile.Domain.Entities;

namespace OrderSphere.UserProfile.Infrastructure.EntityConfigurations;

public sealed class CustomerProfileConfiguration : IEntityTypeConfiguration<CustomerProfile>
{
    public void Configure(EntityTypeBuilder<CustomerProfile> builder)
    {
        builder.ToTable("CustomerProfiles");

        builder.HasKey(p => p.Id);

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => p.KeycloakSubject).IsUnique();

        builder.Property(p => p.KeycloakSubject).IsRequired().HasMaxLength(256);
        builder.Property(p => p.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Email).IsRequired().HasMaxLength(320);

        builder.HasMany(p => p.Addresses)
            .WithOne()
            .HasForeignKey(a => a.CustomerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Addresses).HasField("_addresses").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
