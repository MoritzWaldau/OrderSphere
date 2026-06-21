using OrderSphere.Web.Components;

namespace OrderSphere.Web.Tests.Components;

public sealed class OrderSummaryTests : BunitBase
{

    [Fact]
    public void DiscountRow_Hidden_WhenDiscountIsZero()
    {
        var cut = Render<OrderSummary>(p =>
        {
            p.Add(c => c.Subtotal, 50m);
            p.Add(c => c.Discount, 0m);
        });

        cut.Markup.Should().NotContain("OrderSummary.Discount");
    }

    [Fact]
    public void DiscountRow_Shown_WhenDiscountGreaterThanZero()
    {
        var cut = Render<OrderSummary>(p =>
        {
            p.Add(c => c.Subtotal, 50m);
            p.Add(c => c.Discount, 10m);
        });

        cut.Markup.Should().Contain("OrderSummary.Discount");
        cut.Markup.Should().Contain("−");
    }


    [Fact]
    public void ShippingFreeText_Shown_WhenShippingCostIsZero()
    {
        var cut = Render<OrderSummary>(p =>
        {
            p.Add(c => c.Subtotal, 50m);
            p.Add(c => c.ShippingCost, 0m);
        });

        cut.Markup.Should().Contain("OrderSummary.Free");
    }

    [Fact]
    public void ShippingAmount_Shown_WhenShippingCostGreaterThanZero()
    {
        var cut = Render<OrderSummary>(p =>
        {
            p.Add(c => c.Subtotal, 50m);
            p.Add(c => c.ShippingCost, 4.99m);
        });

        cut.Markup.Should().NotContain("OrderSummary.Free");
        cut.Markup.Should().Contain(Formatting.Currency(4.99m));
    }


    [Fact]
    public void Total_Equals_SubtotalMinusDiscountPlusShippingCost()
    {
        var cut = Render<OrderSummary>(p =>
        {
            p.Add(c => c.Subtotal, 100m);
            p.Add(c => c.Discount, 10m);
            p.Add(c => c.ShippingCost, 4.99m);
        });

        // Expected total: 100 − 10 + 4.99 = 94.99
        cut.Markup.Should().Contain(Formatting.Currency(94.99m));
    }


    [Fact]
    public void LineItems_Hidden_WhenShowLineItemsIsFalse()
    {
        var items = new List<CartItemDto>
        {
            new(Guid.NewGuid(), "Widget", 9.99m, 2)
        };

        var cut = Render<OrderSummary>(p =>
        {
            p.Add(c => c.Subtotal, 19.98m);
            p.Add(c => c.ShowLineItems, false);
            p.Add(c => c.Items, items);
        });

        cut.Markup.Should().NotContain("Widget");
    }

    [Fact]
    public void LineItems_Shown_WhenShowLineItemsIsTrue()
    {
        var items = new List<CartItemDto>
        {
            new(Guid.NewGuid(), "Widget", 9.99m, 2)
        };

        var cut = Render<OrderSummary>(p =>
        {
            p.Add(c => c.Subtotal, 19.98m);
            p.Add(c => c.ShowLineItems, true);
            p.Add(c => c.Items, items);
        });

        cut.Markup.Should().Contain("Widget");
    }
}
