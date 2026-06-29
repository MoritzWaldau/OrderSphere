using OrderSphere.BuildingBlocks.EventBus;

namespace OrderSphere.BuildingBlocks.Contracts.Events;

public sealed record InvoiceGeneratedIntegrationEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required string InvoiceNumber { get; init; }
    public required string CustomerEmail { get; init; }
    public required string CustomerName { get; init; }
    public required decimal Total { get; init; }
    public required string PdfUrl { get; init; }
    public required IReadOnlyList<InvoiceItemDto> Items { get; init; }
}

public sealed record InvoiceItemDto(string ProductName, int Quantity, decimal UnitPrice);
