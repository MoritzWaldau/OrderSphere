using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Domain.Errors;

namespace OrderSphere.Ordering.Domain.Entities;

/// <summary>
/// A return/RMA request raised by a customer against a delivered (or in-fulfilment) order.
/// Plain aggregate (not event-sourced) following the <see cref="Coupon"/> precedent: state
/// transitions are guarded and return <see cref="Result"/> failures rather than throwing, so the
/// application layer can map them to HTTP responses. The state machine is
/// Requested → Approved → Refunded, with Requested → Rejected as the alternate terminal path.
/// </summary>
public sealed class ReturnRequest : AuditableEntity<ReturnRequestId>, IAggregateRoot
{
    public OrderId OrderId { get; private set; }
    public CustomerId CustomerId { get; private set; }
    public ReturnStatus Status { get; private set; }
    public string Reason { get; private set; }

    /// <summary>ISO currency of the order, captured so the refund amount carries its currency.</summary>
    public string Currency { get; private set; }

    /// <summary>Staff note recorded on approval or rejection. Null while the request is open.</summary>
    public string? Resolution { get; private set; }

    public DateTime RequestedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    private readonly List<ReturnItem> _items = [];
    public IReadOnlyCollection<ReturnItem> Items => _items;

    private ReturnRequest()
    {
        OrderId = OrderId.Empty;
        CustomerId = CustomerId.Empty;
        Reason = string.Empty;
        Currency = string.Empty;
    }

    public ReturnRequest(
        OrderId orderId,
        CustomerId customerId,
        string reason,
        string currency,
        IEnumerable<ReturnItem> items,
        DateTime nowUtc)
    {
        Id = ReturnRequestId.New();
        OrderId = orderId;
        CustomerId = customerId;
        Reason = reason;
        Currency = currency;
        Status = ReturnStatus.Requested;
        RequestedAt = nowUtc;
        _items.AddRange(items);
    }

    /// <summary>Sum of the returned line totals — the amount to be refunded on approval.</summary>
    public decimal RefundAmount => _items.Sum(i => i.LineTotal);

    public Result Approve(string? note, DateTime nowUtc)
    {
        if (Status != ReturnStatus.Requested)
            return Result.Failure(ReturnErrors.InvalidStatusTransition);

        Status = ReturnStatus.Approved;
        Resolution = note;
        ResolvedAt = nowUtc;
        return Result.Success();
    }

    public Result Reject(string? note, DateTime nowUtc)
    {
        if (Status != ReturnStatus.Requested)
            return Result.Failure(ReturnErrors.InvalidStatusTransition);

        Status = ReturnStatus.Rejected;
        Resolution = note;
        ResolvedAt = nowUtc;
        return Result.Success();
    }

    /// <summary>
    /// Settles an approved return once Payment confirms the refund. Idempotent: a duplicate
    /// confirmation while already <see cref="ReturnStatus.Refunded"/> is treated as success.
    /// </summary>
    public Result MarkRefunded()
    {
        if (Status == ReturnStatus.Refunded)
            return Result.Success();
        if (Status != ReturnStatus.Approved)
            return Result.Failure(ReturnErrors.InvalidStatusTransition);

        Status = ReturnStatus.Refunded;
        return Result.Success();
    }
}
