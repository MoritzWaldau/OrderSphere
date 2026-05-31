using OrderSphere.Webhooks.Application.Features.Subscriptions.ActivateSubscription;
using OrderSphere.Webhooks.Application.Features.Subscriptions.DeactivateSubscription;

namespace OrderSphere.Webhooks.Tests.Features;

public sealed class ActivateDeactivateCommandHandlerTests
{
    private static readonly CustomerId Owner = CustomerId.New();

    private static WebhookSubscription CreateSubscription() =>
        new(Owner, "https://example.com/hook", "secret", [WebhookEventType.OrderPlaced]);

    // ── Activate: not found ─────────────────────────────────────────────────────

    [Fact]
    public async Task Activate_SubscriptionNotFound_ReturnsNotFound()
    {
        var subs = new List<WebhookSubscription>().AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<IWebhooksDbContext>();
        ctx.Subscriptions.Returns(subs);

        var result = await new ActivateSubscriptionCommandHandler(ctx)
            .Handle(new ActivateSubscriptionCommand(Guid.NewGuid(), Owner.Value), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WebhookErrors.NotFound);
    }

    // ── Activate: sets IsActive = true ──────────────────────────────────────────

    [Fact]
    public async Task Activate_DeactivatedSubscription_SetsIsActiveTrue()
    {
        var sub = CreateSubscription();
        sub.Deactivate();
        var subs = new List<WebhookSubscription> { sub }.AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<IWebhooksDbContext>();
        ctx.Subscriptions.Returns(subs);
        ctx.SaveChangesAsync(default).ReturnsForAnyArgs(1);

        var result = await new ActivateSubscriptionCommandHandler(ctx)
            .Handle(new ActivateSubscriptionCommand(sub.Id.Value, Owner.Value), default);

        result.IsSuccess.Should().BeTrue();
        sub.IsActive.Should().BeTrue();
    }

    // ── Deactivate: not found ───────────────────────────────────────────────────

    [Fact]
    public async Task Deactivate_SubscriptionNotFound_ReturnsNotFound()
    {
        var subs = new List<WebhookSubscription>().AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<IWebhooksDbContext>();
        ctx.Subscriptions.Returns(subs);

        var result = await new DeactivateSubscriptionCommandHandler(ctx)
            .Handle(new DeactivateSubscriptionCommand(Guid.NewGuid(), Owner.Value), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WebhookErrors.NotFound);
    }

    // ── Deactivate: sets IsActive = false ───────────────────────────────────────

    [Fact]
    public async Task Deactivate_ActiveSubscription_SetsIsActiveFalse()
    {
        var sub = CreateSubscription();
        var subs = new List<WebhookSubscription> { sub }.AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<IWebhooksDbContext>();
        ctx.Subscriptions.Returns(subs);
        ctx.SaveChangesAsync(default).ReturnsForAnyArgs(1);

        var result = await new DeactivateSubscriptionCommandHandler(ctx)
            .Handle(new DeactivateSubscriptionCommand(sub.Id.Value, Owner.Value), default);

        result.IsSuccess.Should().BeTrue();
        sub.IsActive.Should().BeFalse();
    }
}
