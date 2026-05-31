using OrderSphere.Ordering.Application.Features.Order.Admin;

namespace OrderSphere.Ordering.Application.Tests.Features;

public sealed class GetAllOrdersQueryHandlerTests
{
    private static readonly Address Addr = new("Max", "Muster", "Str. 1", "Berlin", "10115", "DE");
    private static readonly CustomerId Customer = CustomerId.New();

    private static Order CreateOrder(OrderStatus? advanceTo = null)
    {
        var items = new[] { new OrderItem(ProductId.New(), "Item", Quantity.Of(1), Money.Of(10m)) };
        var o = new Order(Customer, Addr, PaymentMethod.CreditCard, items, Guid.NewGuid());
        if (advanceTo == OrderStatus.Paid || advanceTo == OrderStatus.Shipped || advanceTo == OrderStatus.Delivered)
            o.Confirm("TRK-001");
        if (advanceTo == OrderStatus.Shipped || advanceTo == OrderStatus.Delivered)
            o.MarkShipped();
        if (advanceTo == OrderStatus.Delivered)
            o.MarkDelivered();
        o.PopDomainEvents();
        return o;
    }

    private static GetAllOrdersQueryHandler CreateHandler(IOrderingDbContext ctx) =>
        new(ctx, Substitute.For<ILogger<GetAllOrdersQueryHandler>>());

    // ── No filter — returns all non-deleted ──────────────────────────────────────

    [Fact]
    public async Task Handle_NoFilter_ReturnsAllOrders()
    {
        var o1 = CreateOrder();
        var o2 = CreateOrder(OrderStatus.Paid);
        var orders = new List<Order> { o1, o2 }.AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.Orders.Returns(orders);

        var result = await CreateHandler(ctx).Handle(new(null), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    // ── Status filter applied ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_StatusFilter_ReturnsonlyMatchingOrders()
    {
        var created = CreateOrder();
        var paid    = CreateOrder(OrderStatus.Paid);
        var orders = new List<Order> { created, paid }.AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.Orders.Returns(orders);

        var result = await CreateHandler(ctx).Handle(new(OrderStatus.Paid), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(o => o.Status == OrderStatus.Paid);
    }

    // ── Deleted orders excluded ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_DeletedOrders_NotReturned()
    {
        var o = CreateOrder();
        o.IsDeleted = true;
        var orders = new List<Order> { o }.AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.Orders.Returns(orders);

        var result = await CreateHandler(ctx).Handle(new(null), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
