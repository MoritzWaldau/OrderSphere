namespace OrderSphere.Webhooks.Application.DTOs;

public sealed record DeliveryDto(
    Guid Id,
    string EventType,
    Guid EventId,
    string Status,
    int AttemptCount,
    int? LastHttpStatus,
    string? LastError,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
