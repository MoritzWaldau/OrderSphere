namespace OrderSphere.Catalog.Infrastructure.EntityConfigurations;

public sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("brands");
        builder.HasKey(b => b.Id);
        builder.HasQueryFilter(b => !b.IsDeleted);
        builder.Property(b => b.Name).IsRequired().HasMaxLength(150);
        builder.HasIndex(b => b.Name).IsUnique().HasDatabaseName("IX_brands_name");
        builder.Property(b => b.Slug).IsRequired().HasMaxLength(150);
        builder.HasIndex(b => b.Slug).IsUnique().HasDatabaseName("IX_brands_slug");
        builder.Property(b => b.Description).HasMaxLength(500);
        builder.Property(b => b.LogoUrl).HasMaxLength(500);
        builder.Property(b => b.LogoBlobName).HasMaxLength(500);
        builder.Property(b => b.IsActive).IsRequired().HasDefaultValue(true);
    }
}
