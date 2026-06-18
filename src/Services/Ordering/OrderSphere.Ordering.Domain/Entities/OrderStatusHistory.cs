using OrderSphere.Ordering.Domain.Enums;

namespace OrderSphere.Ordering.Domain.Entities;

/// <summary>
/// An append-only entry in an order's status timeline. Modelled as an owned collection of
/// <see cref="Order"/> (no independent identity); one row is added on every status transition.
/// </summary>
public sealed class OrderStatusHistory
{
    // Client-generated so no store identity is required (works across providers).
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public OrderStatus Status { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string? Note { get; private set; }

    // Parameterless constructor for EF Core materialisation.
    private OrderStatusHistory() { }

    public OrderStatusHistory(OrderStatus status, DateTime occurredAt, string? note = null)
    {
        Status = status;
        OccurredAt = occurredAt;
        Note = note;
    }
}
