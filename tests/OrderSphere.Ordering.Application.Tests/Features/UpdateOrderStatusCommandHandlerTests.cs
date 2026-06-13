using OrderSphere.Ordering.Application.Features.Order.Admin;

namespace OrderSphere.Ordering.Application.Tests.Features;

public sealed class UpdateOrderStatusCommandHandlerTests
{
    // ── Shared test data ────────────────────────────────────────────────────────

    private static readonly Address Addr = new("Max", "Muster", "Hauptstr. 1", "Berlin", "10115", "DE");
    private static readonly CustomerId Customer = CustomerId.New();

    private static Order CreateOrder()
    {
        var items = new[] { new OrderItem(ProductId.New(), "Widget", Quantity.Of(2), Money.Of(5m)) };
        return new Order(Customer, Addr, PaymentMethod.CreditCard, items, Guid.NewGuid());
    }

    private static UpdateOrderStatusCommandHandler CreateHandler(IOrderingDbContext ctx) =>
        new(ctx, Substitute.For<ILogger<UpdateOrderStatusCommandHandler>>());

    // ── Order not found ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsOrderNotFoundError()
    {
        var orders = new List<Order>().BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.Orders.Returns(orders);

        var result = await CreateHandler(ctx).Handle(
            new(Guid.NewGuid(), OrderStatus.Shipped), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.OrderNotFoundError);
    }

    // ── Shipped transition ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShippedStatus_OrderInPaidState_ReturnsSuccess()
    {
        var order = CreateOrder();
        var orderId = order.Id;
        order.Confirm("TRACK-001");
        order.PopDomainEvents();

        var orders = new List<Order> { order }.BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.Orders.Returns(orders);

        var result = await CreateHandler(ctx).Handle(
            new(orderId.Value, OrderStatus.Shipped), default);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Shipped);
    }

    // ── Delivered transition ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_DeliveredStatus_OrderInShippedState_ReturnsSuccess()
    {
        var order = CreateOrder();
        var orderId = order.Id;
        order.Confirm("TRACK-001");
        order.MarkShipped();
        order.PopDomainEvents();

        var orders = new List<Order> { order }.BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.Orders.Returns(orders);

        var result = await CreateHandler(ctx).Handle(
            new(orderId.Value, OrderStatus.Delivered), default);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Delivered);
    }

    // ── Cancelled always fails ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CancelledStatus_ReturnsInvalidStatusTransitionError()
    {
        var order = CreateOrder();
        var orderId = order.Id;

        var orders = new List<Order> { order }.BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.Orders.Returns(orders);

        var result = await CreateHandler(ctx).Handle(
            new(orderId.Value, OrderStatus.Cancelled), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.InvalidStatusTransition);
    }

    // ── Default / unknown status ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_UnknownStatus_ReturnsInvalidStatusTransitionError()
    {
        var order = CreateOrder();
        var orderId = order.Id;

        var orders = new List<Order> { order }.BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.Orders.Returns(orders);

        var result = await CreateHandler(ctx).Handle(
            new(orderId.Value, (OrderStatus)99), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.InvalidStatusTransition);
    }

    // ── Invalid transition (domain throws) ──────────────────────────────────────

    [Fact]
    public async Task Handle_ShippedStatus_OrderInCreatedState_ReturnsInvalidTransitionError()
    {
        // Order is in Created state — MarkShipped should throw InvalidOperationException
        var order = CreateOrder();
        var orderId = order.Id;

        var orders = new List<Order> { order }.BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.Orders.Returns(orders);

        var result = await CreateHandler(ctx).Handle(
            new(orderId.Value, OrderStatus.Shipped), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.InvalidStatusTransition);
    }

    // ── Transaction committed on success ─────────────────────────────────────────

    [Fact]
    public async Task Handle_SuccessfulTransition_CommitsTransaction()
    {
        var order = CreateOrder();
        var orderId = order.Id;
        order.Confirm("TRACK-001");

        var orders = new List<Order> { order }.BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.Orders.Returns(orders);

        await CreateHandler(ctx).Handle(new(orderId.Value, OrderStatus.Shipped), default);

        await ctx.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── Outer exception → RollbackAsync called ───────────────────────────────────

    [Fact]
    public async Task Handle_CommitThrows_RollbackCalledAndReturnsUnknownError()
    {
        var order = CreateOrder();
        var orderId = order.Id;
        order.Confirm("TRACK-001");

        var orders = new List<Order> { order }.BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.Orders.Returns(orders);
        ctx.CommitAsync(Arg.Any<CancellationToken>())
           .ThrowsAsync(new InvalidOperationException("db error"));

        var result = await CreateHandler(ctx).Handle(
            new(orderId.Value, OrderStatus.Shipped), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.UnknownError);
        await ctx.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    }
}
