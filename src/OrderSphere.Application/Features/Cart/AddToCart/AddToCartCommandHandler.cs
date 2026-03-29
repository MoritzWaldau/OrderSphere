using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Entities;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Cart.AddToCart;

public sealed class AddToCartCommandHandler(
    IDbContext context,
    ILogger<AddToCartCommandHandler> logger
) : ICommandHandler<AddToCartCommand, Result>
{
    private bool isNewCart = false;

    public async Task<Result> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await context.BeginTransactionAsync(cancellationToken);

            // Get or create cart
            var cart = await context.Carts.Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.CustomerId == request.CustomerId, cancellationToken);

            if (cart is null)
            {
                cart = new Domain.Entities.Cart(request.CustomerId);
                context.Carts.Add(cart);

                isNewCart = true;
            }

            // Validate product exists and has stock
            var product = await context.Products
                .FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);

            if (product is null)
            {
                logger.LogWarning("Product with id {ProductId} was not found", request.ProductId);
                await context.RollbackAsync(cancellationToken);
                return Result.Failure(ProductErrors.ProductNotFoundError);
            }

            if (product.Stock < request.Quantity)
            {
                logger.LogWarning("Insufficient stock for product {ProductId}. Available: {Stock}, Requested: {Quantity}",
                    request.ProductId, product.Stock, request.Quantity);
                await context.RollbackAsync(cancellationToken);
                return Result.Failure(ProductErrors.InsufficientStockError);
            }

            // Check if item already exists
            var existingItem = cart.Items.FirstOrDefault(x => x.ProductId == request.ProductId);
            if (existingItem != null)
            {
                existingItem.Increase(request.Quantity);
            }
            else
            {
                // Add item to cart with CartId
                var cartItem = new CartItem(request.ProductId, request.Quantity)
                {
                    CartId = cart.Id
                };
                cart.Items.Add(cartItem);
            }

            if(isNewCart)
            {
                await context.Carts.AddAsync(cart, cancellationToken);
            }
            else
            {
                context.Carts.Update(cart);
            }

            
            await context.CommitAsync(cancellationToken);

            logger.LogInformation("Added {Quantity} of product {ProductId} to cart for customer {CustomerId}",
                request.Quantity, request.ProductId, request.CustomerId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            await context.RollbackAsync(cancellationToken);
            logger.LogError(ex, "An error occurred while adding product to cart for customer {CustomerId}",
                request.CustomerId);
            return Result.Failure(CartErrors.UnknownError);
        }
    }
}
