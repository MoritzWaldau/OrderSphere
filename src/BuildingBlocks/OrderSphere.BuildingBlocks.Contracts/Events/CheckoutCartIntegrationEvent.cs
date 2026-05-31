using OrderSphere.BuildingBlocks.EventBus;

namespace OrderSphere.BuildingBlocks.Contracts.Events;

public sealed record CheckoutCartIntegrationEvent : IntegrationEvent
{
    public required Guid CustomerId { get; init; }
    public required string CustomerEmail { get; init; }
    public required string CustomerName { get; init; }
    public required ShippingAddressDto ShippingAddress { get; init; }
    public required string PaymentMethod { get; init; }
    public required IReadOnlyList<OrderItemDto> Items { get; init; }
}

public sealed record ShippingAddressDto(
    string FirstName,
    string LastName,
    string Street,
    string City,
    string PostalCode,
    string Country);

public sealed record OrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal Price);
