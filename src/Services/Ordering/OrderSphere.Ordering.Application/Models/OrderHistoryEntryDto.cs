namespace OrderSphere.Ordering.Application.Models;

/// <summary>
/// Read-model projection of a single order status transition, served from the
/// <c>order_history</c> materialised view rather than the order write aggregate.
/// </summary>
public sealed record OrderHistoryEntryDto(
    Guid Id,
    Guid OrderId,
    Guid CorrelationId,
    string CustomerEmail,
    string PreviousStatus,
    string NewStatus,
    DateTime OccurredAt);
