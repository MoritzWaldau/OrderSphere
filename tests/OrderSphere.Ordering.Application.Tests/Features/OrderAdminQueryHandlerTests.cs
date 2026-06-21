using OrderSphere.Ordering.Application.Features.Order.Admin;
using OrderSphere.Ordering.Application.Tests.Helpers;

namespace OrderSphere.Ordering.Application.Tests.Features;

public sealed class OrderAdminQueryHandlerTests
{
    private static Order CreateOrder(decimal price = 10m, bool paid = false)
    {
        // A fresh Address per order: it is an owned entity and cannot be shared between owners.
        var addr = new Address("Max", "Muster", "Str. 1", "Berlin", "10115", "DE");
        var items = new[] { new OrderItem(ProductId.New(), "Item", Quantity.Of(1), Money.Of(price)) };
        var o = new Order(CustomerId.New(), addr, PaymentMethod.CreditCard, items, Guid.NewGuid());
        if (paid)
            o.Confirm("TRK-001");
        o.PopDomainEvents();
        return o;
    }

    // ── GetOrderByIdAdmin ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderByIdAdmin_Unknown_ReturnsNotFound()
    {
        await using var ctx = OrderingDbContextFactory.Create();

        var handler = new GetOrderByIdAdminQueryHandler(ctx, Substitute.For<ILogger<GetOrderByIdAdminQueryHandler>>());
        var result = await handler.Handle(new(Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.OrderNotFoundError);
    }

    [Fact]
    public async Task GetOrderByIdAdmin_Existing_ReturnsOrderDto()
    {
        await using var ctx = OrderingDbContextFactory.Create();
        var order = CreateOrder();
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        var handler = new GetOrderByIdAdminQueryHandler(ctx, Substitute.For<ILogger<GetOrderByIdAdminQueryHandler>>());
        var result = await handler.Handle(new(order.Id.Value), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(order.Id.Value);
    }

    // ── GetOrderStats ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderStats_NoOrders_ReturnsZeroedStats()
    {
        await using var ctx = OrderingDbContextFactory.Create();

        var handler = new GetOrderStatsQueryHandler(ctx, Substitute.For<ILogger<GetOrderStatsQueryHandler>>());
        var result = await handler.Handle(new(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalOrders.Should().Be(0);
        result.Value.TotalRevenue.Should().Be(0m);
        result.Value.RecentOrders.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrderStats_WithOrders_AggregatesCountsAndRevenue()
    {
        await using var ctx = OrderingDbContextFactory.Create();
        ctx.Orders.Add(CreateOrder(price: 10m));
        ctx.Orders.Add(CreateOrder(price: 15m, paid: true));
        await ctx.SaveChangesAsync();

        var handler = new GetOrderStatsQueryHandler(ctx, Substitute.For<ILogger<GetOrderStatsQueryHandler>>());
        var result = await handler.Handle(new(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalOrders.Should().Be(2);
        result.Value.PendingShipments.Should().Be(1); // the Paid order
        result.Value.TotalRevenue.Should().Be(25m);
        result.Value.RecentOrders.Should().HaveCount(2);
    }
}
