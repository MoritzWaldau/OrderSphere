namespace OrderSphere.Invoicing.Infrastructure.Persistence;

/// <summary>
/// Allocates gapless invoice numbers from the single <c>InvoiceNumberCounters</c> row. Must run inside
/// the caller's transaction (see <see cref="GenerateInvoice"/> handler): on Postgres the row is taken
/// with <c>FOR UPDATE</c> so concurrent generations serialise and the lock is held until commit; the
/// increment is staged on the shared change tracker and persisted by the caller's <c>CommitAsync</c>,
/// so a rollback reverts it. SQLite (test provider) serialises writes at the database level, so a
/// plain read is sufficient there.
/// </summary>
public sealed class SequentialInvoiceNumberGenerator(InvoicingDbContext context) : IInvoiceNumberGenerator
{
    public async Task<string> NextAsync(DateTime issuedAt, CancellationToken ct = default)
    {
        var counter = context.Database.IsNpgsql()
            ? await context.InvoiceNumberCounters
                .FromSqlRaw(@"SELECT * FROM ""InvoiceNumberCounters"" WHERE ""Id"" = 1 FOR UPDATE")
                .SingleAsync(ct)
            : await context.InvoiceNumberCounters.SingleAsync(ct);

        counter.Value += 1;

        // Global running sequence; the year is a prefix only and does not reset the counter.
        return $"INV-{issuedAt:yyyy}-{counter.Value:D6}";
    }
}
