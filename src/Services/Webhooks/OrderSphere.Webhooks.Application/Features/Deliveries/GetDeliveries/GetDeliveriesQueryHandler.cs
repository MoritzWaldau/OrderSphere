using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Webhooks.Application.Features.Deliveries.GetDeliveries;

public sealed class GetDeliveriesQueryHandler(IWebhooksDbContext context)
    : IQueryHandler<GetDeliveriesQuery, Result<IReadOnlyList<DeliveryDto>>>
{
    public async Task<Result<IReadOnlyList<DeliveryDto>>> Handle(
        GetDeliveriesQuery request, CancellationToken cancellationToken)
    {
        var subscriptionId = WebhookSubscriptionId.From(request.SubscriptionId);
        var customerId = CustomerId.From(request.CustomerId);

        // Verify the subscription belongs to the caller.
        var owns = await context.Subscriptions
            .AnyAsync(
                s => s.Id == subscriptionId
                  && s.CustomerId == customerId,
                cancellationToken);

        if (!owns)
            return Result<IReadOnlyList<DeliveryDto>>.Failure(WebhookErrors.NotFound);

        var deliveries = await context.Deliveries
            .Where(d => d.SubscriptionId == subscriptionId)
            .OrderByDescending(d => d.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DeliveryDto(
                d.Id.Value, d.EventType, d.EventId, d.Status.ToString(),
                d.AttemptCount, d.LastHttpStatus, d.LastError,
                d.CreatedAt, d.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<DeliveryDto>>.Success(deliveries);
    }
}
