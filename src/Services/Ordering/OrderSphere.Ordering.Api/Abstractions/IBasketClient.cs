using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Ordering.Api.Abstractions;

public interface IBasketClient
{
    Task<Result<BasketCartInfo>> GetCartAsync(Guid customerId, CancellationToken ct = default);
    Task<Result> ClearCartItemsAsync(Guid customerId, CancellationToken ct = default);
}

public sealed record BasketCartInfo(Guid CustomerId, List<BasketCartItemInfo> Items);
public sealed record BasketCartItemInfo(Guid ProductId, int Quantity);
