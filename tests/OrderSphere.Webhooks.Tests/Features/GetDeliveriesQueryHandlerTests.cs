using OrderSphere.Webhooks.Application.Features.Deliveries.GetDeliveries;
using OrderSphere.Webhooks.Tests.Helpers;

namespace OrderSphere.Webhooks.Tests.Features;

public sealed class GetDeliveriesQueryHandlerTests
{
    private static readonly CustomerId Owner = CustomerId.New();

    private static WebhookSubscription NewSub(CustomerId? owner = null)
        => new(owner ?? Owner, "https://example.com/hook", "secret", [WebhookEventType.OrderPlaced]);

    [Fact]
    public async Task Handle_SubscriptionNotOwnedByCaller_ReturnsNotFound()
    {
        await using var ctx = WebhooksDbContextFactory.Create();
        var sub = NewSub(CustomerId.New());
        ctx.Subscriptions.Add(sub);
        await ctx.SaveChangesAsync();

        var result = await new GetDeliveriesQueryHandler(ctx)
            .Handle(new(sub.Id.Value, Owner.Value, Page: 1, PageSize: 20), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WebhookErrors.NotFound);
    }

    [Fact]
    public async Task Handle_OwnedSubscription_ReturnsDeliveries()
    {
        await using var ctx = WebhooksDbContextFactory.Create();
        var sub = NewSub();
        ctx.Subscriptions.Add(sub);
        ctx.Deliveries.Add(new WebhookDelivery(sub.Id, "OrderPlaced", Guid.NewGuid(), "{}"));
        ctx.Deliveries.Add(new WebhookDelivery(sub.Id, "OrderPlaced", Guid.NewGuid(), "{}"));
        await ctx.SaveChangesAsync();

        var result = await new GetDeliveriesQueryHandler(ctx)
            .Handle(new(sub.Id.Value, Owner.Value, Page: 1, PageSize: 20), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(d => d.EventType == "OrderPlaced" && d.Status == "Pending");
    }

    [Fact]
    public async Task Handle_Pagination_RespectsPageAndPageSize()
    {
        await using var ctx = WebhooksDbContextFactory.Create();
        var sub = NewSub();
        ctx.Subscriptions.Add(sub);
        for (var i = 0; i < 3; i++)
            ctx.Deliveries.Add(new WebhookDelivery(sub.Id, "OrderPlaced", Guid.NewGuid(), "{}"));
        await ctx.SaveChangesAsync();

        var page1 = await new GetDeliveriesQueryHandler(ctx)
            .Handle(new(sub.Id.Value, Owner.Value, Page: 1, PageSize: 2), default);
        var page2 = await new GetDeliveriesQueryHandler(ctx)
            .Handle(new(sub.Id.Value, Owner.Value, Page: 2, PageSize: 2), default);

        page1.Value.Should().HaveCount(2);
        page2.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_OwnedSubscriptionWithoutDeliveries_ReturnsEmpty()
    {
        await using var ctx = WebhooksDbContextFactory.Create();
        var sub = NewSub();
        ctx.Subscriptions.Add(sub);
        await ctx.SaveChangesAsync();

        var result = await new GetDeliveriesQueryHandler(ctx)
            .Handle(new(sub.Id.Value, Owner.Value, Page: 1, PageSize: 20), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
