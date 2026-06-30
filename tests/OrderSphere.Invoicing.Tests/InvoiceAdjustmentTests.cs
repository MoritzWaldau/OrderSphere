using OrderSphere.Invoicing.Domain.Entities;
using OrderSphere.Invoicing.Domain.Enums;
using OrderSphere.Invoicing.Domain.Errors;

namespace OrderSphere.Invoicing.Tests;

public sealed class InvoiceAdjustmentTests
{
    private static readonly DateTime IssuedAt = new(2026, 6, 29, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime AppliedAt = new(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc);

    // Gross 119.00 at 19% VAT -> Net 100.00, Tax 19.00.
    private static Invoice CreateInvoice(decimal total = 119.00m) =>
        Invoice.Create(
            Guid.NewGuid(), "ada@example.com", "Ada Lovelace", total,
            [new InvoiceLineItem { ProductName = "Widget", Quantity = 1, UnitPrice = total }],
            "INV-2026-000001", IssuedAt, taxRate: 0.19m);

    [Fact]
    public void ApplyDiscount_with_valid_amount_reduces_adjusted_amounts_and_sets_status_Adjusted()
    {
        var invoice = CreateInvoice();

        var result = invoice.ApplyDiscount(20.00m, "Goodwill", "admin@ordersphere.dev", AppliedAt);

        result.IsSuccess.Should().BeTrue();
        invoice.Status.Should().Be(InvoiceStatus.Adjusted);
        invoice.AdjustedNet.Should().Be(80.00m);
        invoice.AdjustedTax.Should().Be(15.20m);
        invoice.AdjustedTotal.Should().Be(95.20m);
        // Original amounts stay immutable for revision-safety.
        invoice.NetAmount.Should().Be(100.00m);
        invoice.Total.Should().Be(119.00m);
    }

    [Fact]
    public void IssueCreditNote_with_valid_amount_sets_status_CreditIssued()
    {
        var invoice = CreateInvoice();

        var result = invoice.IssueCreditNote(30.00m, "Defective item", "admin@ordersphere.dev", AppliedAt);

        result.IsSuccess.Should().BeTrue();
        invoice.Status.Should().Be(InvoiceStatus.CreditIssued);
        invoice.AdjustedNet.Should().Be(70.00m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ApplyDiscount_rejects_non_positive_amounts(decimal amount)
    {
        var invoice = CreateInvoice();

        var result = invoice.ApplyDiscount(amount, "Goodwill", "admin@ordersphere.dev", AppliedAt);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InvoicingErrors.AdjustmentAmountInvalid);
        invoice.Adjustments.Should().BeEmpty();
    }

    [Fact]
    public void ApplyDiscount_rejects_an_amount_that_would_drive_the_net_below_zero()
    {
        var invoice = CreateInvoice();

        var result = invoice.ApplyDiscount(150.00m, "Too much", "admin@ordersphere.dev", AppliedAt);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InvoicingErrors.AdjustmentExceedsNet);
        invoice.Adjustments.Should().BeEmpty();
    }

    [Fact]
    public void IssueCreditNote_rejects_an_amount_exceeding_the_already_adjusted_net()
    {
        var invoice = CreateInvoice();
        invoice.ApplyDiscount(60.00m, "First discount", "admin@ordersphere.dev", AppliedAt);

        var result = invoice.IssueCreditNote(50.00m, "Second adjustment", "admin@ordersphere.dev", AppliedAt);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InvoicingErrors.AdjustmentExceedsNet);
        invoice.Adjustments.Should().HaveCount(1);
    }

    [Fact]
    public void Status_reflects_the_most_recently_applied_adjustment_type()
    {
        var invoice = CreateInvoice();

        invoice.ApplyDiscount(10.00m, "Discount", "admin@ordersphere.dev", AppliedAt);
        invoice.Status.Should().Be(InvoiceStatus.Adjusted);

        invoice.IssueCreditNote(10.00m, "Credit", "admin@ordersphere.dev", AppliedAt);
        invoice.Status.Should().Be(InvoiceStatus.CreditIssued);

        invoice.Adjustments.Should().HaveCount(2);
        invoice.AdjustedNet.Should().Be(80.00m);
    }
}
