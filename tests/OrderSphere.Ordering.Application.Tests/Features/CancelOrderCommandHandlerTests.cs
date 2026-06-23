using OrderSphere.Ordering.Application.Features.Order.Admin;

namespace OrderSphere.Ordering.Application.Tests.Features;

public sealed class CancelOrderCommandHandlerTests
{
    private static readonly Address Addr = new("Max", "Muster", "Hauptstr. 1", "Berlin", "10115", "DE");
    private static readonly CustomerId Customer = CustomerId.New();

    private static Order CreateOrder(bool paid = false, bool delivered = false, bool cancelled = false)
    {
        var items = new[] { new OrderItem(ProductId.New(), "Widget", Quantity.Of(2), Money.Of(5m)) };
        var o = Order.Create(Customer, Addr, PaymentMethod.CreditCard, items, Guid.NewGuid());
        if (paid || delivered || cancelled) o.Confirm("TRACK-001");
        if (delivered) o.MarkShipped();
        if (delivered) o.MarkDelivered();
        if (cancelled) o.Cancel();
        return o;
    }

    private static (CancelOrderCommandHandler Handler, IOrderingDbContext Ctx, IOrderEventStore Store) CreateHandler(
        Order? loaded,
        ICatalogClient? catalog = null)
    {
        catalog ??= Substitute.For<ICatalogClient>();
        var ctx = Substitute.For<IOrderingDbContext>();
        var store = Substitute.For<IOrderEventStore>();
        store.LoadAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(loaded);
        var handler = new CancelOrderCommandHandler(ctx, store, catalog, Substitute.For<ILogger<CancelOrderCommandHandler>>());
        return (handler, ctx, store);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsOrderNotFoundError()
    {
        var (handler, _, _) = CreateHandler(loaded: null);

        var result = await handler.Handle(new(Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.OrderNotFoundError);
    }

    [Fact]
    public async Task Handle_OrderDelivered_ReturnsInvalidStatusTransitionError()
    {
        var order = CreateOrder(delivered: true);
        var (handler, _, _) = CreateHandler(order);

        var result = await handler.Handle(new(order.Id.Value), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.InvalidStatusTransition);
    }

    [Fact]
    public async Task Handle_OrderInCreatedState_ReleasesReservation_CancelsSuccessfully()
    {
        // Created → stock was only reserved (not decremented), so the hold is released.
        var order = CreateOrder();
        var catalog = Substitute.For<ICatalogClient>();
        catalog.ReleaseReservationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
               .Returns(Result.Success());
        var (handler, ctx, _) = CreateHandler(order, catalog);

        var result = await handler.Handle(new(order.Id.Value), default);

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
        var catalog = Substitute.For<ICatalogClient>();
        catalog.RestoreStockAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
               .Returns(Result.Success());
        var (handler, _, _) = CreateHandler(order, catalog);

        var result = await handler.Handle(new(order.Id.Value), default);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
        await catalog.Received(1).RestoreStockAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await catalog.DidNotReceive().ReleaseReservationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StockRestoreFails_StillReturnsSuccess()
    {
        var order = CreateOrder(paid: true);
        var catalog = Substitute.For<ICatalogClient>();
        catalog.RestoreStockAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
               .Returns(Result.Failure(new Error("catalog.error", "stock error")));
        var (handler, ctx, _) = CreateHandler(order, catalog);

        var result = await handler.Handle(new(order.Id.Value), default);

        result.IsSuccess.Should().BeTrue();
        await ctx.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SaveChangesThrows_ReturnsUnknownError()
    {
        var order = CreateOrder();
        var catalog = Substitute.For<ICatalogClient>();
        catalog.ReleaseReservationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
               .Returns(Result.Success());
        var (handler, ctx, _) = CreateHandler(order, catalog);
        ctx.SaveChangesAsync(Arg.Any<CancellationToken>())
           .ThrowsAsync(new InvalidOperationException("db error"));

        var result = await handler.Handle(new(order.Id.Value), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.UnknownError);
    }
}
