namespace OrderSphere.Ordering.Application.Models;

public sealed record ReturnRequestDto(
    Guid Id,
    Guid OrderId,
    Guid CustomerId,
    string Status,
    string Reason,
    string? Resolution,
    decimal RefundAmount,
    DateTime RequestedAt,
    DateTime? ResolvedAt,
    IReadOnlyList<ReturnLineDto> Items);

public sealed record ReturnLineDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
