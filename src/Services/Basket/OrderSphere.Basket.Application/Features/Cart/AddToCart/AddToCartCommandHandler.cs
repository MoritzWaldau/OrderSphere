using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Basket.Application.Abstractions;
using OrderSphere.Basket.Domain.Errors;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.ValueObjects;
using CartEntity = OrderSphere.Basket.Domain.Entities.Cart;
using CartItemEntity = OrderSphere.Basket.Domain.Entities.CartItem;

namespace OrderSphere.Basket.Application.Features.Cart.AddToCart;

public sealed class AddToCartCommandHandler(
    IBasketDbContext context,
    ICatalogClient catalogClient,
    ILogger<AddToCartCommandHandler> logger
) : ICommandHandler<AddToCartCommand, Result>
{
    public async Task<Result> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await context.Carts
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.CustomerId == request.CustomerId, cancellationToken);

        var isNewCart = cart is null;
        cart ??= new CartEntity(request.CustomerId);

        var productResult = await catalogClient.GetProductByIdAsync(
            request.ProductId.Value, cancellationToken);

        if (productResult.IsFailure)
        {
            logger.LogWarning("Product {ProductId} not found in Catalog", request.ProductId);
            return Result.Failure(ProductErrors.ProductNotFoundError);
        }

        var product = productResult.Value;
        var existingQty = cart.Items
            .FirstOrDefault(i => i.ProductId == request.ProductId)?.Quantity.Value ?? 0;

        if (existingQty + request.Quantity > product.Stock)
        {
            logger.LogWarning(
                "Insufficient stock for product {ProductId}. Available: {Stock}, InCart: {ExistingQty}, Requested: {Quantity}",
                request.ProductId, product.Stock, existingQty, request.Quantity);
            return Result.Failure(ProductErrors.InsufficientStockError);
        }

        cart.AddItem(new CartItemEntity(request.ProductId, Quantity.Of(request.Quantity)));

        if (isNewCart)
            await context.Carts.AddAsync(cart, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Added {Quantity} of product {ProductId} to cart for customer {CustomerId}",
            request.Quantity, request.ProductId, request.CustomerId);

        return Result.Success();
    }
}
