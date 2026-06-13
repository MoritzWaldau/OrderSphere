using OrderSphere.Ordering.Application.Features.Order;
using OrderSphere.Ordering.Application.Tests.Helpers;

namespace OrderSphere.Ordering.Application.Tests.Features;

public sealed class GetOrderByIdQueryHandlerTests
{
    private static readonly Address Addr = new("Max", "Muster", "Hauptstr. 1", "Berlin", "10115", "DE");
    private static readonly CustomerId Customer = CustomerId.New();

    private static Order CreateOrder()
    {
        var items = new[] { new OrderItem(ProductId.New(), "Widget", Quantity.Of(1), Money.Of(10m)) };
        var o = new Order(Customer, Addr, PaymentMethod.CreditCard, items, Guid.NewGuid());
        o.PopDomainEvents();
        return o;
    }

    private static GetOrderByIdQueryHandler CreateHandler(IOrderingDbContext ctx) =>
        new(ctx, Substitute.For<ILogger<GetOrderByIdQueryHandler>>());

    // ── Order not found ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsOrderNotFoundError()
    {
        var orders = new List<Order>().BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.Orders.Returns(orders);

        var result = await CreateHandler(ctx).Handle(new(Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.OrderNotFoundError);
    }

    // ── Soft-deleted order not returned ─────────────────────────────────────────

    [Fact]
    public async Task Handle_OrderIsDeleted_ReturnsOrderNotFoundError()
    {
        await using var ctx = OrderingDbContextFactory.Create();
        var order = CreateOrder();
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();
        order.IsDeleted = true;
        await ctx.SaveChangesAsync();

        var result = await CreateHandler(ctx).Handle(new(order.Id.Value), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.OrderNotFoundError);
    }

    // ── Happy path ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_OrderExists_ReturnsOrderDto()
    {
        var order = CreateOrder();
        var orders = new List<Order> { order }.BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.Orders.Returns(orders);

        var result = await CreateHandler(ctx).Handle(new(order.Id.Value), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(order.Id.Value);
        result.Value.CustomerId.Should().Be(Customer.Value);
    }
}
