using Microsoft.EntityFrameworkCore;
using OrderSphere.Webhooks.Application.Features.Subscriptions.UpdateSubscription;
using OrderSphere.Webhooks.Tests.Helpers;

namespace OrderSphere.Webhooks.Tests.Features;

public sealed class UpdateSubscriptionCommandHandlerTests
{
    private static readonly CustomerId Owner = CustomerId.New();

    private static WebhookSubscription NewSub()
        => new(Owner, "https://old.example.com", "old-secret", [WebhookEventType.OrderPlaced]);

    [Fact]
    public async Task Handle_UnknownSubscription_ReturnsNotFound()
    {
        await using var ctx = WebhooksDbContextFactory.Create();

        var result = await new UpdateSubscriptionCommandHandler(ctx).Handle(
            new(Guid.NewGuid(), Owner.Value, "https://new", null, [WebhookEventType.OrderPlaced]), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WebhookErrors.NotFound);
    }

    [Fact]
    public async Task Handle_NullSecret_PreservesExistingSecret_AndUpdatesUrl()
    {
        await using var ctx = WebhooksDbContextFactory.Create();
        var sub = NewSub();
        ctx.Subscriptions.Add(sub);
        await ctx.SaveChangesAsync();

        var result = await new UpdateSubscriptionCommandHandler(ctx).Handle(
            new(sub.Id.Value, Owner.Value, "https://new.example.com", null, [WebhookEventType.OrderStatusChanged]), default);

        result.IsSuccess.Should().BeTrue();
        var updated = await ctx.Subscriptions.SingleAsync(s => s.Id == sub.Id);
        updated.Url.Should().Be("https://new.example.com");
        updated.Secret.Should().Be("old-secret");
        updated.Events.Should().Contain("OrderStatusChanged");
    }

    [Fact]
    public async Task Handle_NewSecret_ReplacesSecret()
    {
        await using var ctx = WebhooksDbContextFactory.Create();
        var sub = NewSub();
        ctx.Subscriptions.Add(sub);
        await ctx.SaveChangesAsync();

        var result = await new UpdateSubscriptionCommandHandler(ctx).Handle(
            new(sub.Id.Value, Owner.Value, "https://new.example.com", "new-secret", [WebhookEventType.OrderPlaced]), default);

        result.IsSuccess.Should().BeTrue();
        var updated = await ctx.Subscriptions.SingleAsync(s => s.Id == sub.Id);
        updated.Secret.Should().Be("new-secret");
    }
}
