using OrderSphere.Ordering.Application.Features.Order.Admin;

namespace OrderSphere.Ordering.Application.Tests.Features;

public sealed class CancelOrderCommandHandlerTests
{
    // ── Shared helpers ───────────────────────────────────────────────────────────

    private static readonly Address Addr = new("Max", "Muster", "Hauptstr. 1", "Berlin", "10115", "DE");
    private static readonly CustomerId Customer = CustomerId.New();

    private static Order CreateOrder(bool paid = false, bool delivered = false, bool cancelled = false)
    {
        var items = new[] { new OrderItem(ProductId.New(), "Widget", Quantity.Of(2), Money.Of(5m)) };
        var o = new Order(Customer, Addr, PaymentMethod.CreditCard, items, Guid.NewGuid());
        if (paid || delivered || cancelled) o.Confirm("TRACK-001");
        if (delivered) o.MarkShipped();
        if (delivered) o.MarkDelivered();
        if (cancelled) o.Cancel();
        o.PopDomainEvents();
        return o;
    }

    private static CancelOrderCommandHandler CreateHandler(
        IOrderingDbContext ctx,
        ICatalogClient? catalog = null)
    {
        catalog ??= Substitute.For<ICatalogClient>();
        return new(ctx, catalog, Substitute.For<ILogger<CancelOrderCommandHandler>>());
    }

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

    // ── Cancel from invalid state (Delivered) ────────────────────────────────────

    [Fact]
    public async Task Handle_OrderDelivered_ReturnsInvalidStatusTransitionError()
    {
        var order = CreateOrder(delivered: true);
        var orderId = order.Id;
        var orders = new List<Order> { order }.BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.Orders.Returns(orders);

        var result = await CreateHandler(ctx).Handle(new(orderId.Value), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.InvalidStatusTransition);
    }

    // ── Happy path (Created state) ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_OrderInCreatedState_ReleasesReservation_CancelsSuccessfully()
    {
        // Created → stock was only reserved (not decremented), so the hold is released.
        var order = CreateOrder();
        var orderId = order.Id;
        var orders = new List<Order> { order }.BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.Orders.Returns(orders);

        var catalog = Substitute.For<ICatalogClient>();
        catalog.ReleaseReservationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
               .Returns(Result.Success());

        var result = await CreateHandler(ctx, catalog).Handle(new(orderId.Value), default);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
        await catalog.Received(1).ReleaseReservationAsync(order.CorrelationId, Arg.Any<CancellationToken>());
        await catalog.DidNotReceive().RestoreStockAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await ctx.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderPaid_RestoresOnHandStock_CancelsSuccessfully()
    {
        // Paid → the reservation was confirmed (on-hand stock decremented), so it is restored.
        var order = CreateOrder(paid: true);
        var orderId = order.Id;
        var orders = new List<Order> { order }.BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.Orders.Returns(orders);

        var catalog = Substitute.For<ICatalogClient>();
        catalog.RestoreStockAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
               .Returns(Result.Success());

        var result = await CreateHandler(ctx, catalog).Handle(new(orderId.Value), default);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
        await catalog.Received(1).RestoreStockAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await catalog.DidNotReceive().ReleaseReservationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ── Compensation fails — still completes (logged, not a failure) ─────────────

    [Fact]
    public async Task Handle_StockRestoreFails_StillReturnsSuccess()
    {
        var order = CreateOrder(paid: true);
        var orderId = order.Id;
        var orders = new List<Order> { order }.BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.Orders.Returns(orders);

        var catalog = Substitute.For<ICatalogClient>();
        catalog.RestoreStockAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
               .Returns(Result.Failure(new Error("catalog.error", "stock error")));

        var result = await CreateHandler(ctx, catalog).Handle(new(orderId.Value), default);

        result.IsSuccess.Should().BeTrue();
        await ctx.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Outer exception → UnknownError ─────────────────────────────────────────

    [Fact]
    public async Task Handle_SaveChangesThrows_ReturnsUnknownError()
    {
        var order = CreateOrder();
        var orderId = order.Id;
        var orders = new List<Order> { order }.BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.Orders.Returns(orders);
        ctx.SaveChangesAsync(Arg.Any<CancellationToken>())
           .ThrowsAsync(new InvalidOperationException("db error"));

        var catalog = Substitute.For<ICatalogClient>();
        catalog.ReleaseReservationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
               .Returns(Result.Success());

        var result = await CreateHandler(ctx, catalog).Handle(new(orderId.Value), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.UnknownError);
    }
}
