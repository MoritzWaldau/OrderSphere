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

        builder.HasIndex(p => p.Subject).IsUnique();

        builder.Property(p => p.Subject).IsRequired().HasMaxLength(256);
        builder.Property(p => p.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Email).IsRequired().HasMaxLength(320);
        builder.Property(p => p.IsOnboardingComplete).IsRequired().HasDefaultValue(false);

        // Notification channel preferences — stored as scalar columns on the same table.
        // EmailEnabled defaults true (transactional); SMS/Push default false (DSGVO opt-out).
        builder.OwnsOne(p => p.NotificationPreferences, np =>
        {
            np.Property(x => x.EmailEnabled).HasColumnName("notification_email_enabled").HasDefaultValue(true);
            np.Property(x => x.SmsEnabled).HasColumnName("notification_sms_enabled").HasDefaultValue(false);
            np.Property(x => x.PushEnabled).HasColumnName("notification_push_enabled").HasDefaultValue(false);
            np.Property(x => x.ConsentedAt).HasColumnName("notification_consented_at");
        });

        builder.HasMany(p => p.Addresses)
            .WithOne()
            .HasForeignKey(a => a.CustomerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Addresses).HasField("_addresses").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
