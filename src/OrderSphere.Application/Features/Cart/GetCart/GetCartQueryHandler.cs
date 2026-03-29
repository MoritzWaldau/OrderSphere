using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Cart.GetCart;

public sealed class GetCartQueryHandler(
    IDbContext context,
    ILogger<GetCartQueryHandler> logger
) : IQueryHandler<GetCartQuery, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var cart = await context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId, cancellationToken);

            if (cart is null)
            {
                logger.LogWarning("Cart not found for customer {CustomerId}", request.CustomerId);
                return Result<CartDto>.Failure(CartErrors.CartNotFoundError);
            }

            // Get product details for cart items
            var productIds = cart.Items.Select(ci => ci.ProductId).ToList();
            var products = await context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            var cartItems = cart.Items.Select(ci =>
            {
                var product = products.FirstOrDefault(p => p.Id == ci.ProductId);
                return new CardItemDto(
                    ci.ProductId,
                    product?.Name ?? "Unknown Product",
                    product?.Price ?? 0,
                    ci.Quantity
                );
            }).ToList();

            var cartDto = new CartDto(request.CustomerId, cartItems);
            return Result<CartDto>.Success(cartDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving cart for customer {CustomerId}",
                request.CustomerId);
            return Result<CartDto>.Failure(CartErrors.UnknownError);
        }
    }
}
