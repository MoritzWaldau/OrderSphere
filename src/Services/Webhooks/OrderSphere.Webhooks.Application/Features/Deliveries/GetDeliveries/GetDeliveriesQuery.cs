namespace OrderSphere.Webhooks.Application.Features.Deliveries.GetDeliveries;

public sealed record GetDeliveriesQuery(
    Guid SubscriptionId,
    Guid CustomerId,
    int Page,
    int PageSize)
    : IQuery<Result<IReadOnlyList<DeliveryDto>>>;
