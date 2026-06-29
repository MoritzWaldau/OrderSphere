namespace OrderSphere.Invoicing.Application.Abstractions;

public interface IInvoicingDbContext
{
    DbSet<Invoice> Invoices { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
