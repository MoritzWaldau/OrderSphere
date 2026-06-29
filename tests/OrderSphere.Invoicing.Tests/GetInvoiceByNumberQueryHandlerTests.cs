using OrderSphere.Invoicing.Application.Features.Invoice.GetInvoiceByNumber;
using OrderSphere.Invoicing.Domain.Entities;
using OrderSphere.Invoicing.Infrastructure.Persistence;
using OrderSphere.Invoicing.Tests.Helpers;

namespace OrderSphere.Invoicing.Tests;

public sealed class GetInvoiceByNumberQueryHandlerTests
{
    private static readonly DateTime IssuedAt = new(2026, 6, 29, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_returns_the_invoice_for_a_matching_number()
    {
        using var connection = InvoicingDbContextFactory.NewOpenConnection();
        await using var context = InvoicingDbContextFactory.Create(connection);
        var orderId = Guid.NewGuid();
        await SeedInvoiceAsync(context, "INV-2026-000001", orderId, "Ada Lovelace", "ada@example.com", 119.00m);

        var handler = new GetInvoiceByNumberQueryHandler(context);
        var result = await handler.Handle(new GetInvoiceByNumberQuery("INV-2026-000001"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.InvoiceNumber.Should().Be("INV-2026-000001");
        result.Value.OrderId.Should().Be(orderId);
        result.Value.CustomerName.Should().Be("Ada Lovelace");
        result.Value.CustomerEmail.Should().Be("ada@example.com");
        result.Value.Total.Should().Be(119.00m);
        result.Value.Status.Should().Be("Issued");
    }

    [Fact]
    public async Task Handle_fails_when_no_invoice_has_the_number()
    {
        using var connection = InvoicingDbContextFactory.NewOpenConnection();
        await using var context = InvoicingDbContextFactory.Create(connection);

        var handler = new GetInvoiceByNumberQueryHandler(context);
        var result = await handler.Handle(new GetInvoiceByNumberQuery("INV-2026-999999"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_trims_surrounding_whitespace_from_the_query()
    {
        using var connection = InvoicingDbContextFactory.NewOpenConnection();
        await using var context = InvoicingDbContextFactory.Create(connection);
        await SeedInvoiceAsync(context, "INV-2026-000007", Guid.NewGuid(), "Grace Hopper", "grace@example.com", 49.99m);

        var handler = new GetInvoiceByNumberQueryHandler(context);
        var result = await handler.Handle(new GetInvoiceByNumberQuery("  INV-2026-000007  "), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.InvoiceNumber.Should().Be("INV-2026-000007");
    }

    private static async Task SeedInvoiceAsync(
        InvoicingDbContext context, string number, Guid orderId, string name, string email, decimal total)
    {
        var items = new List<InvoiceLineItem>
        {
            new() { ProductName = "Widget", Quantity = 1, UnitPrice = total },
        };
        var invoice = Invoice.Create(orderId, email, name, total, items, number, IssuedAt);
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();
    }
}
