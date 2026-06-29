namespace OrderSphere.Invoicing.Application.Abstractions;

/// <summary>
/// Allocates the next sequential, gapless invoice number (format <c>INV-{year}-{6-digit}</c>).
/// The allocation must run inside the same transaction as the invoice insert: the counter is locked
/// for the duration of that transaction, so a rollback reverts the increment and numbering stays
/// gapless. Call only between <see cref="IInvoicingDbContext.BeginTransactionAsync"/> and
/// <see cref="IInvoicingDbContext.CommitAsync"/>.
/// </summary>
public interface IInvoiceNumberGenerator
{
    Task<string> NextAsync(DateTime issuedAt, CancellationToken ct = default);
}
