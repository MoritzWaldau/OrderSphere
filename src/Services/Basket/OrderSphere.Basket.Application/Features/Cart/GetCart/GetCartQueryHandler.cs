using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Basket.Application.Abstractions;
using OrderSphere.Basket.Application.DTOs;
using OrderSphere.Basket.Domain.Errors;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Basket.Application.Features.Cart.GetCart;

public sealed class GetCartQueryHandler(
    IBasketDbContext context,
    ICatalogClient catalogClient,
    ILogger<GetCartQueryHandler> logger
) : IQueryHandler<GetCartQuery, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var cart = await context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId, cancellationToken);

        if (cart is null)
        {
            logger.LogWarning("Cart not found for customer {CustomerId}", request.CustomerId);
            return Result<CartDto>.Failure(CartErrors.CartNotFoundError);
        }

        var productIds = cart.Items.Select(ci => ci.ProductId.Value).ToList();
        var namesResult = await catalogClient.GetProductNamesByIdsAsync(productIds, cancellationToken);
        var names = namesResult.IsSuccess ? namesResult.Value : new Dictionary<Guid, string>();

        var items = cart.Items.Select(ci =>
        {
            names.TryGetValue(ci.ProductId.Value, out var name);
            return new CartItemDto(ci.ProductId.Value, name ?? "Unknown Product", 0m, ci.Quantity);
        }).ToList();

        return Result<CartDto>.Success(new CartDto(request.CustomerId.Value, items));
    }
}
