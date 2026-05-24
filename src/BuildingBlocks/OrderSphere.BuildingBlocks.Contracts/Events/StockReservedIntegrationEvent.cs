using OrderSphere.BuildingBlocks.EventBus;

namespace OrderSphere.BuildingBlocks.Contracts.Events;

public sealed record StockReservedIntegrationEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required IReadOnlyList<ReservedStockItemDto> ReservedItems { get; init; }
}

public sealed record ReservedStockItemDto(Guid ProductId, int Quantity);
