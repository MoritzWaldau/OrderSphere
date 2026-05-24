using OrderSphere.BuildingBlocks.EventBus;

namespace OrderSphere.BuildingBlocks.Contracts.Events;

public sealed record PaymentProcessedIntegrationEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required bool Succeeded { get; init; }
    public string? FailureReason { get; init; }
    public string? TransactionId { get; init; }
    public required string CustomerEmail { get; init; }
    public required string PaymentMethod { get; init; }
}
