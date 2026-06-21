namespace OrderSphere.Webhooks.Tests.Domain;

public sealed class WebhookSubscriptionTests
{
    private static readonly CustomerId Owner = CustomerId.New();

    private static WebhookSubscription CreateSubscription(params WebhookEventType[] eventTypes) =>
        new(Owner, "https://example.com/hook", "secret", eventTypes);


    [Fact]
    public void Constructor_IsActiveByDefault()
    {
        var sub = CreateSubscription(WebhookEventType.OrderPlaced);

        sub.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Constructor_SetsOwnerAndUrl()
    {
        var sub = CreateSubscription(WebhookEventType.OrderPlaced);

        sub.CustomerId.Should().Be(Owner);
        sub.Url.Should().Be("https://example.com/hook");
    }


    [Fact]
    public void GetEventTypes_EmptyEvents_ReturnsEmptyList()
    {
        var sub = CreateSubscription();

        var types = sub.GetEventTypes();

        types.Should().BeEmpty();
    }

    [Fact]
    public void GetEventTypes_SingleEvent_ReturnsOneElement()
    {
        var sub = CreateSubscription(WebhookEventType.OrderPlaced);

        var types = sub.GetEventTypes();

        types.Should().ContainSingle().Which.Should().Be(WebhookEventType.OrderPlaced);
    }

    [Fact]
    public void GetEventTypes_MultipleEvents_ReturnsAll()
    {
        var sub = CreateSubscription(
            WebhookEventType.OrderPlaced,
            WebhookEventType.PaymentCompleted,
            WebhookEventType.OrderStatusChanged);

        var types = sub.GetEventTypes();

        types.Should().HaveCount(3);
        types.Should().Contain(WebhookEventType.OrderPlaced);
        types.Should().Contain(WebhookEventType.PaymentCompleted);
        types.Should().Contain(WebhookEventType.OrderStatusChanged);
    }


    [Fact]
    public void ListensTo_SubscribedEventType_ReturnsTrue()
    {
        var sub = CreateSubscription(WebhookEventType.OrderPlaced);

        sub.ListensTo(WebhookEventType.OrderPlaced).Should().BeTrue();
    }

    [Fact]
    public void ListensTo_UnsubscribedEventType_ReturnsFalse()
    {
        var sub = CreateSubscription(WebhookEventType.OrderPlaced);

        sub.ListensTo(WebhookEventType.PaymentFailed).Should().BeFalse();
    }

    [Fact]
    public void ListensTo_EmptySubscription_ReturnsFalse()
    {
        var sub = CreateSubscription();

        sub.ListensTo(WebhookEventType.OrderPlaced).Should().BeFalse();
    }


    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var sub = CreateSubscription(WebhookEventType.OrderPlaced);

        sub.Deactivate();

        sub.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_AfterDeactivate_SetsIsActiveTrue()
    {
        var sub = CreateSubscription(WebhookEventType.OrderPlaced);
        sub.Deactivate();

        sub.Activate();

        sub.IsActive.Should().BeTrue();
    }


    [Fact]
    public void Update_ChangesUrlSecretAndEvents()
    {
        var sub = CreateSubscription(WebhookEventType.OrderPlaced);

        sub.Update("https://new.example.com/hook", "new-secret",
            [WebhookEventType.PaymentCompleted, WebhookEventType.PaymentFailed]);

        sub.Url.Should().Be("https://new.example.com/hook");
        sub.Secret.Should().Be("new-secret");
        sub.GetEventTypes().Should().HaveCount(2);
        sub.GetEventTypes().Should().Contain(WebhookEventType.PaymentCompleted);
    }

    [Fact]
    public void Update_ClearsOldEvents()
    {
        var sub = CreateSubscription(WebhookEventType.OrderPlaced);

        sub.Update("https://example.com/hook", "secret", [WebhookEventType.PaymentFailed]);

        sub.ListensTo(WebhookEventType.OrderPlaced).Should().BeFalse();
    }


    [Fact]
    public void Delete_SetsIsDeletedTrue()
    {
        var sub = CreateSubscription(WebhookEventType.OrderPlaced);

        sub.Delete();

        sub.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Delete_SetsIsActiveFalse()
    {
        var sub = CreateSubscription(WebhookEventType.OrderPlaced);

        sub.Delete();

        sub.IsActive.Should().BeFalse();
    }
}
