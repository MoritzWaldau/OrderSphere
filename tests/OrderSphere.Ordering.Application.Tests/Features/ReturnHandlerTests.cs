using Microsoft.EntityFrameworkCore;
using OrderSphere.Ordering.Application.Features.Returns.ApproveReturn;
using OrderSphere.Ordering.Application.Features.Returns.GetReturnsByCustomer;
using OrderSphere.Ordering.Application.Features.Returns.RejectReturn;
using OrderSphere.Ordering.Application.Features.Returns.RequestReturn;
using OrderSphere.Ordering.Application.Tests.Helpers;
using OrderSphere.Ordering.Infrastructure.Persistence;

namespace OrderSphere.Ordering.Application.Tests.Features;

public sealed class ReturnHandlerTests
{
    private static readonly ProductId Product = ProductId.New();
    private static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private static OrderView SeedPaidOrder(OrderingDbContext ctx, CustomerId customer, int quantity = 2, decimal price = 10m)
    {
        var address = new Address("Max", "Muster", "Hauptstr. 1", "Berlin", "10115", "DE");
        var items = new[] { new OrderItem(Product, "Widget", Quantity.Of(quantity), Money.Of(price, "EUR")) };
        var order = OrderView.Create(OrderId.New(), customer, address, PaymentMethod.CreditCard, Guid.NewGuid(), items, Now);
        order.Confirm("TRACK-1", Now); // Created → Paid (returnable)
        ctx.Orders.Add(order);
        ctx.SaveChanges();
        return order;
    }

    [Fact]
    public async Task RequestReturn_ValidOwnerAndQuantity_CreatesRequestWithRefundAmount()
    {
        await using var ctx = OrderingDbContextFactory.Create();
        var customer = CustomerId.New();
        var order = SeedPaidOrder(ctx, customer);

        var result = await new RequestReturnCommandHandler(ctx).Handle(
            new RequestReturnCommand(order.Id.Value, customer.Value, "Defekt",
                [new RequestReturnLine(Product.Value, 1)]), default);

        result.IsSuccess.Should().BeTrue();
        var stored = await ctx.ReturnRequests.Include(r => r.Items).SingleAsync();
        stored.Status.Should().Be(ReturnStatus.Requested);
        stored.Currency.Should().Be("EUR");
        stored.RefundAmount.Should().Be(10m);
        stored.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task RequestReturn_DifferentCustomer_ReturnsNotOrderOwner()
    {
        await using var ctx = OrderingDbContextFactory.Create();
        var order = SeedPaidOrder(ctx, CustomerId.New());

        var result = await new RequestReturnCommandHandler(ctx).Handle(
            new RequestReturnCommand(order.Id.Value, Guid.NewGuid(), "Defekt",
                [new RequestReturnLine(Product.Value, 1)]), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReturnErrors.NotOrderOwner);
    }

    [Fact]
    public async Task RequestReturn_QuantityExceedsOrdered_Fails()
    {
        await using var ctx = OrderingDbContextFactory.Create();
        var customer = CustomerId.New();
        var order = SeedPaidOrder(ctx, customer, quantity: 2);

        var result = await new RequestReturnCommandHandler(ctx).Handle(
            new RequestReturnCommand(order.Id.Value, customer.Value, "Defekt",
                [new RequestReturnLine(Product.Value, 3)]), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReturnErrors.QuantityExceedsOrdered);
    }

    [Fact]
    public async Task RequestReturn_UnknownProduct_Fails()
    {
        await using var ctx = OrderingDbContextFactory.Create();
        var customer = CustomerId.New();
        var order = SeedPaidOrder(ctx, customer);

        var result = await new RequestReturnCommandHandler(ctx).Handle(
            new RequestReturnCommand(order.Id.Value, customer.Value, "Defekt",
                [new RequestReturnLine(Guid.NewGuid(), 1)]), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReturnErrors.UnknownItem);
    }

    [Fact]
    public async Task RequestReturn_UnknownOrder_ReturnsOrderNotFound()
    {
        await using var ctx = OrderingDbContextFactory.Create();

        var result = await new RequestReturnCommandHandler(ctx).Handle(
            new RequestReturnCommand(Guid.NewGuid(), Guid.NewGuid(), "Defekt",
                [new RequestReturnLine(Product.Value, 1)]), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReturnErrors.OrderNotFound);
    }

    [Fact]
    public async Task ApproveReturn_PendingRequest_ApprovesAndStagesRefundOutbox()
    {
        await using var ctx = OrderingDbContextFactory.Create();
        var customer = CustomerId.New();
        var order = SeedPaidOrder(ctx, customer);
        var request = new ReturnRequest(order.Id, customer, "Defekt", "EUR",
            [new ReturnItem(Product, "Widget", 1, 10m)], Now);
        ctx.ReturnRequests.Add(request);
        await ctx.SaveChangesAsync();

        var result = await new ApproveReturnCommandHandler(ctx).Handle(
            new ApproveReturnCommand(request.Id.Value, "genehmigt"), default);

        result.IsSuccess.Should().BeTrue();
        var stored = await ctx.ReturnRequests.SingleAsync();
        stored.Status.Should().Be(ReturnStatus.Approved);
        stored.Resolution.Should().Be("genehmigt");
    }

    [Fact]
    public async Task ApproveReturn_Unknown_ReturnsNotFound()
    {
        await using var ctx = OrderingDbContextFactory.Create();

        var result = await new ApproveReturnCommandHandler(ctx).Handle(
            new ApproveReturnCommand(Guid.NewGuid(), null), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReturnErrors.NotFound);
    }

    [Fact]
    public async Task RejectReturn_PendingRequest_TransitionsToRejected()
    {
        await using var ctx = OrderingDbContextFactory.Create();
        var customer = CustomerId.New();
        var request = new ReturnRequest(OrderId.New(), customer, "Defekt", "EUR",
            [new ReturnItem(Product, "Widget", 1, 10m)], Now);
        ctx.ReturnRequests.Add(request);
        await ctx.SaveChangesAsync();

        var result = await new RejectReturnCommandHandler(ctx).Handle(
            new RejectReturnCommand(request.Id.Value, "kein Anspruch"), default);

        result.IsSuccess.Should().BeTrue();
        (await ctx.ReturnRequests.SingleAsync()).Status.Should().Be(ReturnStatus.Rejected);
    }

    [Fact]
    public async Task GetReturnsByCustomer_ReturnsOnlyOwnRequests()
    {
        await using var ctx = OrderingDbContextFactory.Create();
        var customer = CustomerId.New();
        ctx.ReturnRequests.Add(new ReturnRequest(OrderId.New(), customer, "A", "EUR",
            [new ReturnItem(Product, "Widget", 1, 10m)], Now));
        ctx.ReturnRequests.Add(new ReturnRequest(OrderId.New(), CustomerId.New(), "B", "EUR",
            [new ReturnItem(Product, "Widget", 1, 10m)], Now));
        await ctx.SaveChangesAsync();

        var result = await new GetReturnsByCustomerQueryHandler(ctx).Handle(
            new GetReturnsByCustomerQuery(customer.Value), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].Reason.Should().Be("A");
    }
}
