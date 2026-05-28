using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSphere.Basket.Api.CatalogClient;
using OrderSphere.Basket.Domain.Errors;
using OrderSphere.Basket.Infrastructure.Persistence;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.ValueObjects;
using CartEntity = OrderSphere.Basket.Domain.Entities.Cart;
using CartItemEntity = OrderSphere.Basket.Domain.Entities.CartItem;

namespace OrderSphere.Basket.Api.Features.Cart;

public sealed class AddToCartCommandHandler(
    BasketDbContext context,
    ICatalogClient catalogClient,
    ILogger<AddToCartCommandHandler> logger
) : IRequestHandler<AddToCartCommand, Result>
{
    public async Task<Result> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var cart = await context.Carts
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.CustomerId == request.CustomerId, cancellationToken);

            var isNewCart = cart is null;
            cart ??= new CartEntity(request.CustomerId);

            // ICatalogClient still uses Guid — convert at the service boundary.
            var productResult = await catalogClient.GetProductByIdAsync(
                request.ProductId.Value, cancellationToken);

            if (productResult.IsFailure)
            {
                logger.LogWarning("Product {ProductId} not found in Catalog", request.ProductId);
                return Result.Failure(ProductErrors.ProductNotFoundError);
            }

            var product = productResult.Value;
            if (product.Stock < request.Quantity)
            {
                logger.LogWarning(
                    "Insufficient stock for product {ProductId}. Available: {Stock}, Requested: {Quantity}",
                    request.ProductId, product.Stock, request.Quantity);
                return Result.Failure(ProductErrors.InsufficientStockError);
            }

            // Route through the aggregate method so CartItemAddedDomainEvent is raised.
            cart.AddItem(new CartItemEntity(request.ProductId, Quantity.Of(request.Quantity)));

            if (isNewCart)
                await context.Carts.AddAsync(cart, cancellationToken);
            else
                context.Carts.Update(cart);

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Added {Quantity} of product {ProductId} to cart for customer {CustomerId}",
                request.Quantity, request.ProductId, request.CustomerId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding product to cart for customer {CustomerId}", request.CustomerId);
            return Result.Failure(CartErrors.UnknownError);
        }
    }
}
