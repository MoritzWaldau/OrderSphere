namespace OrderSphere.Invoicing.Application.Abstractions;

public interface IInvoicingDbContext
{
    DbSet<Invoice> Invoices { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
