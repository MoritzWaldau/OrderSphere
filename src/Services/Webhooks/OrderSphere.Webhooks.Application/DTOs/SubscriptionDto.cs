namespace OrderSphere.Webhooks.Application.DTOs;

public sealed record SubscriptionDto(
    Guid Id,
    string Url,
    string Events,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
