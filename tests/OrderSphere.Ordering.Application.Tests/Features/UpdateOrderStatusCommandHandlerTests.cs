using OrderSphere.Ordering.Application.Features.Order.Admin;

namespace OrderSphere.Ordering.Application.Tests.Features;

public sealed class UpdateOrderStatusCommandHandlerTests
{
    private static readonly Address Addr = new("Max", "Muster", "Hauptstr. 1", "Berlin", "10115", "DE");
    private static readonly CustomerId Customer = CustomerId.New();

    private static Order CreateOrder()
    {
        var items = new[] { new OrderItem(ProductId.New(), "Widget", Quantity.Of(2), Money.Of(5m)) };
        return Order.Create(Customer, Addr, PaymentMethod.CreditCard, items, Guid.NewGuid());
    }

    private static (UpdateOrderStatusCommandHandler Handler, IOrderingDbContext Ctx, IOrderEventStore Store) CreateHandler(Order? loaded)
    {
        var ctx = Substitute.For<IOrderingDbContext>();
        var store = Substitute.For<IOrderEventStore>();
        store.LoadAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(loaded);
        var handler = new UpdateOrderStatusCommandHandler(ctx, store, Substitute.For<ILogger<UpdateOrderStatusCommandHandler>>());
        return (handler, ctx, store);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsOrderNotFoundError()
    {
        var (handler, _, _) = CreateHandler(loaded: null);

        var result = await handler.Handle(new(Guid.NewGuid(), OrderStatus.Shipped), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.OrderNotFoundError);
    }

    [Fact]
    public async Task Handle_ShippedStatus_OrderInPaidState_ReturnsSuccess()
    {
        var order = CreateOrder();
        order.Confirm("TRACK-001");
        var (handler, _, _) = CreateHandler(order);

        var result = await handler.Handle(new(order.Id.Value, OrderStatus.Shipped), default);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public async Task Handle_DeliveredStatus_OrderInShippedState_ReturnsSuccess()
    {
        var order = CreateOrder();
        order.Confirm("TRACK-001");
        order.MarkShipped();
        var (handler, _, _) = CreateHandler(order);

        var result = await handler.Handle(new(order.Id.Value, OrderStatus.Delivered), default);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Delivered);
    }

    [Fact]
    public async Task Handle_CancelledStatus_ReturnsInvalidStatusTransitionError()
    {
        var order = CreateOrder();
        var (handler, _, _) = CreateHandler(order);

        var result = await handler.Handle(new(order.Id.Value, OrderStatus.Cancelled), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.InvalidStatusTransition);
    }

    [Fact]
    public async Task Handle_UnknownStatus_ReturnsInvalidStatusTransitionError()
    {
        var order = CreateOrder();
        var (handler, _, _) = CreateHandler(order);

        var result = await handler.Handle(new(order.Id.Value, (OrderStatus)99), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.InvalidStatusTransition);
    }

    [Fact]
    public async Task Handle_ShippedStatus_OrderInCreatedState_ReturnsInvalidTransitionError()
    {
        // Order is in Created state — MarkShipped should throw InvalidOperationException.
        var order = CreateOrder();
        var (handler, _, _) = CreateHandler(order);

        var result = await handler.Handle(new(order.Id.Value, OrderStatus.Shipped), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.InvalidStatusTransition);
    }

    [Fact]
    public async Task Handle_SuccessfulTransition_AppendsAndSavesChanges()
    {
        var order = CreateOrder();
        order.Confirm("TRACK-001");
        var (handler, ctx, store) = CreateHandler(order);

        await handler.Handle(new(order.Id.Value, OrderStatus.Shipped), default);

        await store.Received(1).AppendAsync(order, Arg.Any<CancellationToken>());
        await ctx.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SaveChangesThrows_ReturnsUnknownError()
    {
        var order = CreateOrder();
        order.Confirm("TRACK-001");
        var (handler, ctx, _) = CreateHandler(order);
        ctx.SaveChangesAsync(Arg.Any<CancellationToken>())
           .ThrowsAsync(new InvalidOperationException("db error"));

        var result = await handler.Handle(new(order.Id.Value, OrderStatus.Shipped), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.UnknownError);
    }
}
