using Microsoft.Extensions.Logging.Abstractions;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Features.Cart.GetCart;
using OrderSphere.Domain.Errors;
using CartEntity = OrderSphere.Domain.Entities.Cart;
using CartItemEntity = OrderSphere.Domain.Entities.CartItem;
using ProductEntity = OrderSphere.Domain.Entities.Product;

namespace OrderSphere.Application.Tests.Features.Cart;

public class GetCartQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenCartMissing_ReturnsCartNotFound()
    {
        var cartsDbSet = new List<CartEntity>().AsQueryable().BuildMockDbSet();
        var context = Substitute.For<IDbContext>();
        context.Carts.Returns(cartsDbSet);

        var handler = new GetCartQueryHandler(context, NullLogger<GetCartQueryHandler>.Instance);

        var result = await handler.Handle(new GetCartQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CartErrors.CartNotFoundError);
    }

    [Fact]
    public async Task Handle_HappyPath_ProjectsItemsWithProductDetails()
    {
        var customerId = Guid.NewGuid();
        var product = new ProductEntity("Widget", "x", 9.99m, 100);
        var cart = new CartEntity(customerId);
        cart.Items.Add(new CartItemEntity(product.Id, 4));

        var cartsDbSet = new[] { cart }.AsQueryable().BuildMockDbSet();
        var productsDbSet = new[] { product }.AsQueryable().BuildMockDbSet();
        var context = Substitute.For<IDbContext>();
        context.Carts.Returns(cartsDbSet);
        context.Products.Returns(productsDbSet);

        var handler = new GetCartQueryHandler(context, NullLogger<GetCartQueryHandler>.Instance);

        var result = await handler.Handle(new GetCartQuery(customerId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items[0].ProductName.Should().Be("Widget");
        result.Value.Items[0].Price.Should().Be(9.99m);
        result.Value.Items[0].Quantity.Should().Be(4);
    }
}
