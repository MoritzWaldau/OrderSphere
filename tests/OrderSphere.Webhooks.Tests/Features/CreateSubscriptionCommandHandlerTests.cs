using OrderSphere.Webhooks.Application.Features.Subscriptions.CreateSubscription;

namespace OrderSphere.Webhooks.Tests.Features;

public sealed class CreateSubscriptionCommandHandlerTests
{
    private static readonly CustomerId Owner = CustomerId.New();

    private static IWebhooksDbContext MakeContext()
    {
        var subs = new List<WebhookSubscription>().BuildMockDbSet();
        var ctx = Substitute.For<IWebhooksDbContext>();
        ctx.Subscriptions.Returns(subs);
        ctx.SaveChangesAsync(default).ReturnsForAnyArgs(1);
        return ctx;
    }


    [Fact]
    public async Task Handle_ValidRequest_ReturnsCreatedDto()
    {
        var ctx = MakeContext();

        var result = await new CreateSubscriptionCommandHandler(ctx).Handle(
            new CreateSubscriptionCommand(Owner.Value, "https://example.com/hook", null,
                [WebhookEventType.OrderPlaced]),
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().NotBeEmpty();
        result.Value.Secret.Should().NotBeNullOrWhiteSpace();
    }


    [Fact]
    public async Task Handle_NoSecret_GeneratesSecret()
    {
        var ctx = MakeContext();

        var result = await new CreateSubscriptionCommandHandler(ctx).Handle(
            new CreateSubscriptionCommand(Owner.Value, "https://example.com/hook", null,
                [WebhookEventType.OrderPlaced]),
            default);

        result.Value.Secret.Should().NotBeNullOrWhiteSpace();
    }


    [Fact]
    public async Task Handle_SecretProvided_UsesProvidedSecret()
    {
        var ctx = MakeContext();

        var result = await new CreateSubscriptionCommandHandler(ctx).Handle(
            new CreateSubscriptionCommand(Owner.Value, "https://example.com/hook", "my-secret",
                [WebhookEventType.OrderPlaced]),
            default);

        result.Value.Secret.Should().Be("my-secret");
    }
}
