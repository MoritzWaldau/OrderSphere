using OrderSphere.BuildingBlocks.EventBus;

namespace OrderSphere.BuildingBlocks.Contracts.Events;

public sealed record OrderPlacedIntegrationEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required string CustomerEmail { get; init; }
    public required string CustomerName { get; init; }
    public required string TrackingNumber { get; init; }
    public required string ShippingFirstName { get; init; }
    public required string ShippingLastName { get; init; }
    public required string ShippingStreet { get; init; }
    public required string ShippingCity { get; init; }
    public required string ShippingPostalCode { get; init; }
    public required string ShippingCountry { get; init; }
    public required IReadOnlyList<OrderPlacedItemDto> Items { get; init; }
    public required decimal Total { get; init; }
}

public sealed record OrderPlacedItemDto(string ProductName, int Quantity, decimal Price);
