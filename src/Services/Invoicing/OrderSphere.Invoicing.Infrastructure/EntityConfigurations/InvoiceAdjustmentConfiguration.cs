namespace OrderSphere.Invoicing.Infrastructure.EntityConfigurations;

public sealed class InvoiceAdjustmentConfiguration : IEntityTypeConfiguration<InvoiceAdjustment>
{
    public void Configure(EntityTypeBuilder<InvoiceAdjustment> builder)
    {
        builder.ToTable("InvoiceAdjustments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.AmountNet).HasPrecision(18, 2);
        builder.Property(a => a.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(a => a.AppliedBy).HasMaxLength(256).IsRequired();

        builder.HasOne<Invoice>()
            .WithMany(i => i.Adjustments)
            .HasForeignKey(a => a.InvoiceId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
