using Microsoft.EntityFrameworkCore;
using OrderSphere.Basket.Application.Abstractions;
using OrderSphere.Basket.Application.DTOs;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Basket.Application.Features.Cart.GetCart;

public sealed class GetCartQueryHandler(
    IBasketDbContext context,
    ICatalogClient catalogClient
) : IQueryHandler<GetCartQuery, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var cart = await context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId, cancellationToken);

        if (cart is null)
            return Result<CartDto>.Success(new CartDto(request.CustomerId.Value, []));

        var productIds = cart.Items.Select(ci => ci.ProductId.Value).ToList();
        var infosResult = await catalogClient.GetProductInfosByIdsAsync(productIds, cancellationToken);
        var infos = infosResult.IsSuccess
            ? infosResult.Value
            : new Dictionary<Guid, CatalogProductInfo>();

        var items = cart.Items.Select(ci =>
        {
            infos.TryGetValue(ci.ProductId.Value, out var info);
            return new CartItemDto(
                ci.ProductId.Value,
                info?.Name ?? "Unknown Product",
                info?.Price ?? 0m,
                ci.Quantity);
        }).ToList();

        return Result<CartDto>.Success(new CartDto(request.CustomerId.Value, items));
    }
}
