using OrderSphere.Webhooks.Application.Features.Subscriptions.CreateSubscription;

namespace OrderSphere.Webhooks.Tests.Features;

public sealed class CreateSubscriptionCommandValidatorTests
{
    private readonly CreateSubscriptionCommandValidator _validator = new();

    private static CreateSubscriptionCommand ValidCommand(
        string url = "https://example.com/hook",
        WebhookEventType[]? events = null) =>
        new(Guid.NewGuid(), url, null, events ?? [WebhookEventType.OrderPlaced]);

    // ── URL validation ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_EmptyUrl_Fails()
    {
        var result = await _validator.ValidateAsync(ValidCommand(url: ""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_NonHttpsUrl_Fails()
    {
        var result = await _validator.ValidateAsync(ValidCommand(url: "http://example.com/hook"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_RelativeUrl_Fails()
    {
        var result = await _validator.ValidateAsync(ValidCommand(url: "/relative/path"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_HttpsUrl_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand(url: "https://example.com/hook"));
        result.IsValid.Should().BeTrue();
    }

    // ── Events validation ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_EmptyEvents_Fails()
    {
        var result = await _validator.ValidateAsync(ValidCommand(events: []));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_AtLeastOneEvent_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand(events: [WebhookEventType.OrderPlaced]));
        result.IsValid.Should().BeTrue();
    }
}
