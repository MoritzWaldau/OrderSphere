using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSphere.Advisory.Domain.Entities;

namespace OrderSphere.Advisory.Infrastructure.EntityConfigurations;

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");

        builder.HasKey(c => c.Id);

        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.Property(c => c.ConversationKey).IsRequired().HasMaxLength(64);
        builder.Property(c => c.CustomerSub).IsRequired().HasMaxLength(256);
        builder.Property(c => c.SerializedSession).HasColumnType("text");

        // One conversation per (customer, client key).
        builder.HasIndex(c => new { c.CustomerSub, c.ConversationKey }).IsUnique();

        builder.HasMany(c => c.Messages)
            .WithOne()
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
