using OrderSphere.BuildingBlocks.EventBus;

namespace OrderSphere.BuildingBlocks.Contracts.Events;

/// <summary>
/// Published once by UserProfile (system of record for the customer) when a GDPR erasure
/// request is confirmed. Every service holding PII for this customer consumes it from its own
/// fan-out queue and anonymizes (or, where no retention obligation applies, deletes) its data.
/// </summary>
public sealed record CustomerErasureRequestedIntegrationEvent : IntegrationEvent
{
    public required string CustomerSub { get; init; }
    public required string CustomerEmail { get; init; }
}
