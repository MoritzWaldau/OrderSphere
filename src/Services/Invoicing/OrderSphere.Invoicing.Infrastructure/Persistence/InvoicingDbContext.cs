using OrderSphere.BuildingBlocks.Extensions;

namespace OrderSphere.Invoicing.Infrastructure.Persistence;

public sealed class InvoicingDbContext(DbContextOptions<InvoicingDbContext> options)
    : DbContext(options), IInvoicingDbContext
{
    public DbSet<Invoice> Invoices => Set<Invoice>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.ApplyAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<InvoiceId>().HaveConversion<InvoiceIdConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoicingDbContext).Assembly);
}
