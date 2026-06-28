namespace OrderSphere.Ordering.Domain.Enums;

/// <summary>
/// Lifecycle of a return request (RMA). Terminal states are <see cref="Rejected"/> and
/// <see cref="Refunded"/>. Allowed transitions: Requested → Approved → Refunded, and
/// Requested → Rejected.
/// </summary>
public enum ReturnStatus
{
    Requested,
    Approved,
    Rejected,
    Refunded
}
