using Microsoft.EntityFrameworkCore;
using OrderSphere.Basket.Application.Abstractions;
using OrderSphere.Basket.Application.DTOs;
using OrderSphere.Basket.Domain.Errors;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Basket.Application.Features.Cart.GetCartInternal;

public sealed class GetCartInternalQueryHandler(
    IBasketDbContext context
) : IQueryHandler<GetCartInternalQuery, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(GetCartInternalQuery request, CancellationToken cancellationToken)
    {
        var cart = await context.Carts
            .AsNoTracking()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId, cancellationToken);

        if (cart is null)
            return Result<CartDto>.Failure(CartErrors.CartNotFoundError);

        var items = cart.Items
            .Select(ci => new CartItemDto(ci.ProductId.Value, string.Empty, 0m, ci.Quantity))
            .ToList();

        return Result<CartDto>.Success(new CartDto(request.CustomerId.Value, items));
    }
}
