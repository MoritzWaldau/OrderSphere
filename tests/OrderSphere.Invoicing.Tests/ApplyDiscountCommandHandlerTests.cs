using OrderSphere.Invoicing.Application.Features.Invoice.ApplyDiscount;
using OrderSphere.Invoicing.Domain.Entities;
using OrderSphere.Invoicing.Tests.Helpers;

namespace OrderSphere.Invoicing.Tests;

public sealed class ApplyDiscountCommandHandlerTests
{
    private static readonly DateTime IssuedAt = new(2026, 6, 29, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_with_percentage_resolves_against_the_currently_effective_net()
    {
        using var connection = InvoicingDbContextFactory.NewOpenConnection();
        await using var context = InvoicingDbContextFactory.Create(connection);

        // Gross 119.00 at 19% VAT -> Net 100.00.
        var invoice = Invoice.Create(
            Guid.NewGuid(), "ada@example.com", "Ada Lovelace", 119.00m,
            [new InvoiceLineItem { ProductName = "Widget", Quantity = 1, UnitPrice = 119.00m }],
            "INV-2026-000001", IssuedAt, taxRate: 0.19m);
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        var handler = new ApplyDiscountCommandHandler(context);
        var result = await handler.Handle(
            new ApplyDiscountCommand(invoice.Id.Value, AbsoluteAmount: null, PercentageValue: 10m, "Loyalty", "admin@ordersphere.dev"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AdjustedNet.Should().Be(90.00m);
        result.Value.Status.Should().Be("Adjusted");
        result.Value.Adjustments.Should().ContainSingle(a => a.AmountNet == 10.00m);
    }

    [Fact]
    public async Task Handle_fails_when_the_invoice_does_not_exist()
    {
        using var connection = InvoicingDbContextFactory.NewOpenConnection();
        await using var context = InvoicingDbContextFactory.Create(connection);

        var handler = new ApplyDiscountCommandHandler(context);
        var result = await handler.Handle(
            new ApplyDiscountCommand(Guid.NewGuid(), 10m, null, "Loyalty", "admin@ordersphere.dev"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
