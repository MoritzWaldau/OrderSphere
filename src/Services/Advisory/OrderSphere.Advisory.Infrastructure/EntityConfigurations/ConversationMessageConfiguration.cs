using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSphere.Advisory.Domain.Entities;

namespace OrderSphere.Advisory.Infrastructure.EntityConfigurations;

public sealed class ConversationMessageConfiguration : IEntityTypeConfiguration<ConversationMessage>
{
    public void Configure(EntityTypeBuilder<ConversationMessage> builder)
    {
        builder.ToTable("ConversationMessages");

        builder.HasKey(m => m.Id);

        builder.HasQueryFilter(m => !m.IsDeleted);

        builder.Property(m => m.Role).IsRequired().HasMaxLength(32);
        builder.Property(m => m.Text).IsRequired().HasColumnType("text");

        builder.HasIndex(m => m.ConversationId);
    }
}
