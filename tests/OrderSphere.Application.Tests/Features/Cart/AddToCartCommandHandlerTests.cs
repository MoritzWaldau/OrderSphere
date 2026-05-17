using Microsoft.Extensions.Logging.Abstractions;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Features.Cart.AddToCart;
using OrderSphere.Domain.Errors;
using CartEntity = OrderSphere.Domain.Entities.Cart;
using ProductEntity = OrderSphere.Domain.Entities.Product;

namespace OrderSphere.Application.Tests.Features.Cart;

public class AddToCartCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenProductNotFound_ReturnsProductNotFoundAndRollsBack()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var cartsDbSet = new[] { new CartEntity(customerId) }.AsQueryable().BuildMockDbSet();
        var productsDbSet = new List<ProductEntity>().AsQueryable().BuildMockDbSet();
        var context = Substitute.For<IDbContext>();
        context.Carts.Returns(cartsDbSet);
        context.Products.Returns(productsDbSet);

        var handler = new AddToCartCommandHandler(context, NullLogger<AddToCartCommandHandler>.Instance);

        var result = await handler.Handle(new AddToCartCommand(customerId, productId, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.ProductNotFoundError);
        await context.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenStockInsufficient_ReturnsInsufficientStockAndRollsBack()
    {
        var customerId = Guid.NewGuid();
        var product = new ProductEntity("Widget", "x", 10m, stock: 2);

        var cartsDbSet2 = new[] { new CartEntity(customerId) }.AsQueryable().BuildMockDbSet();
        var productsDbSet2 = new[] { product }.AsQueryable().BuildMockDbSet();
        var context = Substitute.For<IDbContext>();
        context.Carts.Returns(cartsDbSet2);
        context.Products.Returns(productsDbSet2);

        var handler = new AddToCartCommandHandler(context, NullLogger<AddToCartCommandHandler>.Instance);

        var result = await handler.Handle(new AddToCartCommand(customerId, product.Id, 5), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.InsufficientStockError);
        await context.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HappyPath_CommitsTransaction()
    {
        var customerId = Guid.NewGuid();
        var product = new ProductEntity("Widget", "x", 10m, stock: 50);
        var cart = new CartEntity(customerId);

        var cartsDbSet3 = new[] { cart }.AsQueryable().BuildMockDbSet();
        var productsDbSet3 = new[] { product }.AsQueryable().BuildMockDbSet();
        var context = Substitute.For<IDbContext>();
        context.Carts.Returns(cartsDbSet3);
        context.Products.Returns(productsDbSet3);

        var handler = new AddToCartCommandHandler(context, NullLogger<AddToCartCommandHandler>.Instance);

        var result = await handler.Handle(new AddToCartCommand(customerId, product.Id, 3), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await context.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await context.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        cart.Items.Should().ContainSingle(i => i.ProductId == product.Id && i.Quantity == 3);
    }
}
