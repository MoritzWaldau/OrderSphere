using OrderSphere.Ordering.Application.Features.Order;

namespace OrderSphere.Ordering.Application.Tests.Features;

public sealed class GetOrderByCorrelationIdQueryHandlerTests
{
    private static readonly Address Addr = new("Max", "Muster", "Str. 1", "Berlin", "10115", "DE");
    private static readonly CustomerId Customer = CustomerId.New();

    private static Order CreateOrder(Guid correlationId)
    {
        var items = new[] { new OrderItem(ProductId.New(), "Item", Quantity.Of(1), Money.Of(10m)) };
        var o = new Order(Customer, Addr, PaymentMethod.CreditCard, items, correlationId);
        o.PopDomainEvents();
        return o;
    }

    private static GetOrderByCorrelationIdQueryHandler CreateHandler(IOrderingDbContext ctx) =>
        new(ctx, Substitute.For<ILogger<GetOrderByCorrelationIdQueryHandler>>());

    // ── Order not yet persisted → returns Success(null) ─────────────────────────

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsSuccessWithNullValue()
    {
        var orders = new List<Order>().AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.Orders.Returns(orders);

        var result = await CreateHandler(ctx).Handle(new(Guid.NewGuid()), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    // ── Happy path ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_OrderExists_ReturnsOrderDto()
    {
        var correlationId = Guid.NewGuid();
        var order = CreateOrder(correlationId);
        var orders = new List<Order> { order }.AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.Orders.Returns(orders);

        var result = await CreateHandler(ctx).Handle(new(correlationId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(order.Id.Value);
    }
}
