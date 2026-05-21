using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Domain.Primitives;
using OrderSphere.Ordering.Api.Abstractions;
using OrderSphere.Ordering.Api.Models;
using OrderSphere.Ordering.Domain.Errors;
using OrderSphere.Ordering.Infrastructure.Persistence;

namespace OrderSphere.Ordering.Api.Features.Cart;

public sealed class GetCartQueryHandler(
    IOrderingDbContext context,
    ICatalogClient catalogClient,
    ILogger<GetCartQueryHandler> logger
) : IRequestHandler<GetCartQuery, Result<CartDto>>
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

            var productIds = cart.Items.Select(ci => ci.ProductId).ToList();
            var namesResult = await catalogClient.GetProductNamesByIdsAsync(productIds, cancellationToken);
            var names = namesResult.IsSuccess ? namesResult.Value : new Dictionary<Guid, string>();

            var items = cart.Items.Select(ci =>
            {
                names.TryGetValue(ci.ProductId, out var name);
                return new CartItemDto(ci.ProductId, name ?? "Unknown Product", 0m, ci.Quantity);
            }).ToList();

            return Result<CartDto>.Success(new CartDto(request.CustomerId, items));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving cart for customer {CustomerId}", request.CustomerId);
            return Result<CartDto>.Failure(CartErrors.UnknownError);
        }
    }
}
