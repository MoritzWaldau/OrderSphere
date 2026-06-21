using OrderSphere.Webhooks.Application.Features.Subscriptions.GetSubscription;
using OrderSphere.Webhooks.Tests.Helpers;

namespace OrderSphere.Webhooks.Tests.Features;

public sealed class GetSubscriptionQueryHandlerTests
{
    private static readonly CustomerId Owner = CustomerId.New();

    private static WebhookSubscription NewSub(CustomerId? owner = null)
        => new(owner ?? Owner, "https://example.com/hook", "secret", [WebhookEventType.OrderPlaced]);

    [Fact]
    public async Task Handle_ExistingOwnedSubscription_ReturnsDto()
    {
        await using var ctx = WebhooksDbContextFactory.Create();
        var sub = NewSub();
        ctx.Subscriptions.Add(sub);
        await ctx.SaveChangesAsync();

        var result = await new GetSubscriptionQueryHandler(ctx)
            .Handle(new(sub.Id.Value, Owner.Value), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(sub.Id.Value);
        result.Value.Url.Should().Be("https://example.com/hook");
    }

    [Fact]
    public async Task Handle_SubscriptionOwnedByAnotherCustomer_ReturnsNotFound()
    {
        await using var ctx = WebhooksDbContextFactory.Create();
        var sub = NewSub(CustomerId.New());
        ctx.Subscriptions.Add(sub);
        await ctx.SaveChangesAsync();

        var result = await new GetSubscriptionQueryHandler(ctx)
            .Handle(new(sub.Id.Value, Owner.Value), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WebhookErrors.NotFound);
    }

    [Fact]
    public async Task Handle_UnknownSubscription_ReturnsNotFound()
    {
        await using var ctx = WebhooksDbContextFactory.Create();

        var result = await new GetSubscriptionQueryHandler(ctx)
            .Handle(new(Guid.NewGuid(), Owner.Value), default);

        result.IsFailure.Should().BeTrue();
    }
}
