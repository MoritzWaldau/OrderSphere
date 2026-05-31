using OrderSphere.Webhooks.Application.Features.Subscriptions.GetSubscriptions;

namespace OrderSphere.Webhooks.Tests.Features;

public sealed class GetSubscriptionsQueryHandlerTests
{
    private static readonly CustomerId Owner = CustomerId.New();
    private static readonly CustomerId OtherOwner = CustomerId.New();

    private static WebhookSubscription CreateSubscription(CustomerId? owner = null) =>
        new(owner ?? Owner, "https://example.com/hook", "secret", [WebhookEventType.OrderPlaced]);

    // ── Returns only the caller's subscriptions ─────────────────────────────────

    [Fact]
    public async Task Handle_ReturnsOnlyCallerSubscriptions()
    {
        var own = CreateSubscription(Owner);
        var other = CreateSubscription(OtherOwner);
        var subs = new List<WebhookSubscription> { own, other }.AsQueryable().BuildMockDbSet();

        var ctx = Substitute.For<IWebhooksDbContext>();
        ctx.Subscriptions.Returns(subs);

        var result = await new GetSubscriptionsQueryHandler(ctx)
            .Handle(new GetSubscriptionsQuery(Owner.Value), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Id.Should().Be(own.Id.Value);
    }

    // ── Soft-deleted subscriptions are excluded ─────────────────────────────────

    [Fact]
    public async Task Handle_ExcludesSoftDeletedSubscriptions()
    {
        var active = CreateSubscription();
        var deleted = CreateSubscription();
        deleted.Delete();

        var subs = new List<WebhookSubscription> { active, deleted }.AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<IWebhooksDbContext>();
        ctx.Subscriptions.Returns(subs);

        var result = await new GetSubscriptionsQueryHandler(ctx)
            .Handle(new GetSubscriptionsQuery(Owner.Value), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    // ── Empty list is a success ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoSubscriptions_ReturnsEmptySuccess()
    {
        var subs = new List<WebhookSubscription>().AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<IWebhooksDbContext>();
        ctx.Subscriptions.Returns(subs);

        var result = await new GetSubscriptionsQueryHandler(ctx)
            .Handle(new GetSubscriptionsQuery(Owner.Value), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
