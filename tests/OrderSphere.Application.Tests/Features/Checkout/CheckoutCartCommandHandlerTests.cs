using Microsoft.Extensions.Logging.Abstractions;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Features.Checkout;
using OrderSphere.Application.Models;
using OrderSphere.Application.Models.Events;
using OrderSphere.Application.ServiceBus;
using CartEntity = OrderSphere.Domain.Entities.Cart;
using CartItemEntity = OrderSphere.Domain.Entities.CartItem;
using ProductEntity = OrderSphere.Domain.Entities.Product;
using OrderSphere.Domain.Enums;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.ValueObjects;

namespace OrderSphere.Application.Tests.Features.Checkout;

public class CheckoutCartCommandHandlerTests
{
    private static CheckoutCartDto Dto(Guid customerId) =>
        new(customerId,
            new Address("Jane", "Doe", "Main 1", "Berlin", "10115", "DE"),
            PaymentMethod.Invoice);

    [Fact]
    public async Task Handle_WhenCartNotFound_ReturnsCartNotFoundError()
    {
        var customerId = Guid.NewGuid();
        var cartsDbSet = new List<CartEntity>().AsQueryable().BuildMockDbSet();
        var context = Substitute.For<IDbContext>();
        context.Carts.Returns(cartsDbSet);
        var publisher = Substitute.For<IServiceBusPublisher>();

        var handler = new CheckoutCartCommandHandler(context, publisher, NullLogger<CheckoutCartCommandHandler>.Instance);

        var result = await handler.Handle(new CheckoutCartCommand(Dto(customerId)), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CartErrors.CartNotFoundError);
        await publisher.DidNotReceiveWithAnyArgs().PublishCheckoutCartEventAsync(default!);
    }

    [Fact]
    public async Task Handle_WhenCartIsEmpty_ReturnsEmptyCartError()
    {
        var customerId = Guid.NewGuid();
        var cart = new CartEntity(customerId);
        var cartsDbSet = new[] { cart }.AsQueryable().BuildMockDbSet();
        var context = Substitute.For<IDbContext>();
        context.Carts.Returns(cartsDbSet);
        var publisher = Substitute.For<IServiceBusPublisher>();

        var handler = new CheckoutCartCommandHandler(context, publisher, NullLogger<CheckoutCartCommandHandler>.Instance);

        var result = await handler.Handle(new CheckoutCartCommand(Dto(customerId)), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CheckoutCartErrors.EmptyCartError);
        await publisher.DidNotReceiveWithAnyArgs().PublishCheckoutCartEventAsync(default!);
    }

    [Fact]
    public async Task Handle_WhenPublishFails_RollsBackTransactionAndReturnsUnknownError()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = new CartEntity(customerId);
        cart.Items.Add(new CartItemEntity(productId, 2));
        var product = new ProductEntity("Widget", "x", 10m, 100);

        var cartsDbSet = new[] { cart }.AsQueryable().BuildMockDbSet();
        var productsDbSet = new[] { product }.AsQueryable().BuildMockDbSet();
        var cartItemsDbSet = new List<CartItemEntity>().AsQueryable().BuildMockDbSet();
        var context = Substitute.For<IDbContext>();
        context.Carts.Returns(cartsDbSet);
        context.Products.Returns(productsDbSet);
        context.CartItems.Returns(cartItemsDbSet);

        var publisher = Substitute.For<IServiceBusPublisher>();
        publisher.PublishCheckoutCartEventAsync(Arg.Any<CheckoutCartEvent>())
            .Returns(_ => Task.FromException(new InvalidOperationException("bus down")));

        var handler = new CheckoutCartCommandHandler(context, publisher, NullLogger<CheckoutCartCommandHandler>.Instance);

        var result = await handler.Handle(new CheckoutCartCommand(Dto(customerId)), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CheckoutCartErrors.UnknownError);
        await context.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await context.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
