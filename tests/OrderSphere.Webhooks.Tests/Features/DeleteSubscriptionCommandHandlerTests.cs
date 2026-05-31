using OrderSphere.Webhooks.Application.Features.Subscriptions.DeleteSubscription;

namespace OrderSphere.Webhooks.Tests.Features;

public sealed class DeleteSubscriptionCommandHandlerTests
{
    private static readonly CustomerId Owner = CustomerId.New();
    private static readonly CustomerId OtherOwner = CustomerId.New();

    private static WebhookSubscription CreateSubscription(CustomerId? owner = null) =>
        new(owner ?? Owner, "https://example.com/hook", "secret", [WebhookEventType.OrderPlaced]);

    // ── Subscription not found returns failure ──────────────────────────────────

    [Fact]
    public async Task Handle_SubscriptionNotFound_ReturnsNotFound()
    {
        var subs = new List<WebhookSubscription>().AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<IWebhooksDbContext>();
        ctx.Subscriptions.Returns(subs);

        var result = await new DeleteSubscriptionCommandHandler(ctx)
            .Handle(new DeleteSubscriptionCommand(Guid.NewGuid(), Owner.Value), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WebhookErrors.NotFound);
    }

    // ── Cannot delete another customer's subscription ───────────────────────────

    [Fact]
    public async Task Handle_DifferentCustomer_ReturnsNotFound()
    {
        var sub = CreateSubscription(OtherOwner);
        var subs = new List<WebhookSubscription> { sub }.AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<IWebhooksDbContext>();
        ctx.Subscriptions.Returns(subs);

        var result = await new DeleteSubscriptionCommandHandler(ctx)
            .Handle(new DeleteSubscriptionCommand(sub.Id.Value, Owner.Value), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WebhookErrors.NotFound);
    }

    // ── Already soft-deleted subscription not visible ────────────────────────────

    [Fact]
    public async Task Handle_AlreadyDeleted_ReturnsNotFound()
    {
        var sub = CreateSubscription();
        sub.Delete();
        var subs = new List<WebhookSubscription> { sub }.AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<IWebhooksDbContext>();
        ctx.Subscriptions.Returns(subs);

        var result = await new DeleteSubscriptionCommandHandler(ctx)
            .Handle(new DeleteSubscriptionCommand(sub.Id.Value, Owner.Value), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WebhookErrors.NotFound);
    }

    // ── Happy path ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidRequest_SoftDeletesAndReturnsSuccess()
    {
        var sub = CreateSubscription();
        var subs = new List<WebhookSubscription> { sub }.AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<IWebhooksDbContext>();
        ctx.Subscriptions.Returns(subs);
        ctx.SaveChangesAsync(default).ReturnsForAnyArgs(1);

        var result = await new DeleteSubscriptionCommandHandler(ctx)
            .Handle(new DeleteSubscriptionCommand(sub.Id.Value, Owner.Value), default);

        result.IsSuccess.Should().BeTrue();
        sub.IsDeleted.Should().BeTrue();
        sub.IsActive.Should().BeFalse();
    }
}
