using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSphere.Basket.Api.CatalogClient;
using OrderSphere.Basket.Domain.Errors;
using OrderSphere.Basket.Infrastructure.Persistence;
using OrderSphere.BuildingBlocks.Primitives;

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
            cart ??= new Domain.Entities.Cart(request.CustomerId);

            var productResult = await catalogClient.GetProductByIdAsync(request.ProductId, cancellationToken);
            if (productResult.IsFailure)
            {
                logger.LogWarning("Product {ProductId} not found in Catalog", request.ProductId);
                return Result.Failure(ProductErrors.ProductNotFoundError);
            }

            var product = productResult.Value;
            if (product.Stock < request.Quantity)
            {
                logger.LogWarning("Insufficient stock for product {ProductId}. Available: {Stock}, Requested: {Quantity}",
                    request.ProductId, product.Stock, request.Quantity);
                return Result.Failure(ProductErrors.InsufficientStockError);
            }

            var existingItem = cart.Items.FirstOrDefault(x => x.ProductId == request.ProductId);
            if (existingItem is not null)
            {
                existingItem.Increase(request.Quantity);
            }
            else
            {
                var cartItem = new Domain.Entities.CartItem(request.ProductId, request.Quantity) { CartId = cart.Id };
                cart.Items.Add(cartItem);
            }

            if (isNewCart)
                await context.Carts.AddAsync(cart, cancellationToken);
            else
                context.Carts.Update(cart);

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Added {Quantity} of product {ProductId} to cart for customer {CustomerId}",
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
