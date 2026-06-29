using OrderSphere.Invoicing.Infrastructure.Persistence;
using OrderSphere.Invoicing.Tests.Helpers;

namespace OrderSphere.Invoicing.Tests;

public sealed class InvoiceNumberGeneratorTests
{
    private static readonly DateTime IssuedAt = new(2026, 6, 29, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task NextAsync_produces_sequential_zero_padded_numbers_with_year_prefix()
    {
        using var connection = InvoicingDbContextFactory.NewOpenConnection();
        await using var context = InvoicingDbContextFactory.Create(connection);
        var generator = new SequentialInvoiceNumberGenerator(context);

        var first = await generator.NextAsync(IssuedAt);
        await context.SaveChangesAsync();
        var second = await generator.NextAsync(IssuedAt);
        await context.SaveChangesAsync();

        first.Should().Be("INV-2026-000001");
        second.Should().Be("INV-2026-000002");
    }

    [Fact]
    public async Task NextAsync_continues_the_running_sequence_across_context_instances()
    {
        using var connection = InvoicingDbContextFactory.NewOpenConnection();

        string third;
        await using (var context = InvoicingDbContextFactory.Create(connection))
        {
            var generator = new SequentialInvoiceNumberGenerator(context);
            await generator.NextAsync(IssuedAt);
            await generator.NextAsync(IssuedAt);
            await context.SaveChangesAsync();
        }

        // A fresh context (e.g. a later message) must keep counting from the persisted value.
        await using (var context = InvoicingDbContextFactory.Create(connection))
        {
            var generator = new SequentialInvoiceNumberGenerator(context);
            third = await generator.NextAsync(IssuedAt);
            await context.SaveChangesAsync();
        }

        third.Should().Be("INV-2026-000003");
    }

    [Fact]
    public async Task NextAsync_uses_the_issue_year_as_prefix()
    {
        using var connection = InvoicingDbContextFactory.NewOpenConnection();
        await using var context = InvoicingDbContextFactory.Create(connection);
        var generator = new SequentialInvoiceNumberGenerator(context);

        var number = await generator.NextAsync(new DateTime(2027, 1, 2, 0, 0, 0, DateTimeKind.Utc));

        number.Should().Be("INV-2027-000001");
    }
}
