using OrderSphere.Ordering.Application.Features.Order;
using OrderSphere.Ordering.Application.Tests.Helpers;

namespace OrderSphere.Ordering.Application.Tests.Features;

public sealed class GetOrdersByCustomerQueryHandlerTests
{
    private static readonly Address Addr = new("Max", "Muster", "Str. 1", "Berlin", "10115", "DE");
    private static readonly CustomerId CustomerA = CustomerId.New();
    private static readonly CustomerId CustomerB = CustomerId.New();

    private static Order CreateOrder(CustomerId customerId)
    {
        var items = new[] { new OrderItem(ProductId.New(), "Item", Quantity.Of(1), Money.Of(9m)) };
        var o = new Order(customerId, Addr, PaymentMethod.CreditCard, items, Guid.NewGuid());
        o.PopDomainEvents();
        return o;
    }

    private static GetOrdersByCustomerQueryHandler CreateHandler(IOrderingDbContext ctx) =>
        new(ctx, Substitute.For<ILogger<GetOrdersByCustomerQueryHandler>>());

    // ── No orders for customer → empty list ─────────────────────────────────────

    [Fact]
    public async Task Handle_NoOrders_ReturnsEmptyList()
    {
        var orders = new List<Order>().AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.Orders.Returns(orders);

        var result = await CreateHandler(ctx).Handle(new(CustomerA.Value), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    // ── Deleted orders excluded ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_DeletedOrders_NotReturned()
    {
        await using var ctx = OrderingDbContextFactory.Create();
        var o1 = CreateOrder(CustomerA);
        ctx.Orders.Add(o1);
        await ctx.SaveChangesAsync();
        o1.IsDeleted = true;
        await ctx.SaveChangesAsync();

        var result = await CreateHandler(ctx).Handle(new(CustomerA.Value), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    // ── Orders for other customer not returned ───────────────────────────────────

    [Fact]
    public async Task Handle_OrdersBelongingToOtherCustomer_NotReturned()
    {
        var o1 = CreateOrder(CustomerB);
        var orders = new List<Order> { o1 }.AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.Orders.Returns(orders);

        var result = await CreateHandler(ctx).Handle(new(CustomerA.Value), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    // ── Happy path ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_TwoOrdersForCustomer_ReturnsBoth()
    {
        var o1 = CreateOrder(CustomerA);
        var o2 = CreateOrder(CustomerA);
        var orders = new List<Order> { o1, o2 }.AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.Orders.Returns(orders);

        var result = await CreateHandler(ctx).Handle(new(CustomerA.Value), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }
}
