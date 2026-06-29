namespace OrderSphere.Invoicing.Infrastructure.EntityConfigurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
        builder.HasIndex(i => i.OrderId).IsUnique();

        builder.Property(i => i.CustomerEmail).HasMaxLength(256).IsRequired();
        builder.Property(i => i.CustomerName).HasMaxLength(256).IsRequired();
        builder.Property(i => i.Total).HasPrecision(18, 2);
        builder.Property(i => i.BlobPath).HasMaxLength(512);

        builder.OwnsMany(i => i.Items, items => items.ToJson("items"));

        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}
