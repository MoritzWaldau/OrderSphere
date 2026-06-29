using OrderSphere.Invoicing.Infrastructure.Persistence;

namespace OrderSphere.Invoicing.Infrastructure.EntityConfigurations;

public sealed class InvoiceNumberCounterConfiguration : IEntityTypeConfiguration<InvoiceNumberCounter>
{
    public void Configure(EntityTypeBuilder<InvoiceNumberCounter> builder)
    {
        builder.ToTable("InvoiceNumberCounters");
        builder.HasKey(c => c.Id);

        // Fixed single-row identifier; never database-generated.
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Value).IsRequired();

        // Seed the one counter row so the first allocation returns 1.
        builder.HasData(new InvoiceNumberCounter { Id = 1, Value = 0 });
    }
}
