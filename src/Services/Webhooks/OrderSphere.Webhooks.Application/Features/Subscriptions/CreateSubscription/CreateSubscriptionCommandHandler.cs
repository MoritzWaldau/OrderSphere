using System.Security.Cryptography;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Webhooks.Domain.Entities;

namespace OrderSphere.Webhooks.Application.Features.Subscriptions.CreateSubscription;

public sealed class CreateSubscriptionCommandHandler(IWebhooksDbContext context)
    : ICommandHandler<CreateSubscriptionCommand, Result<SubscriptionCreatedDto>>
{
    public async Task<Result<SubscriptionCreatedDto>> Handle(
        CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        // 24 random bytes → exactly 32 Base64 characters (no padding).
        var secret = string.IsNullOrWhiteSpace(request.Secret)
            ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
            : request.Secret;

        var subscription = new WebhookSubscription(
            CustomerId.From(request.CustomerId),
            request.Url,
            secret,
            request.Events);

        context.Subscriptions.Add(subscription);
        await context.SaveChangesAsync(cancellationToken);

        return Result<SubscriptionCreatedDto>.Success(
            new SubscriptionCreatedDto(subscription.Id.Value, secret));
    }
}
