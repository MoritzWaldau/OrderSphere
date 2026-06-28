namespace OrderSphere.Payment.Infrastructure.Providers;

/// <summary>
/// Stripe configuration. When <see cref="ApiKey"/> is set, the real Stripe provider is
/// registered under the "CreditCard" method name in place of the simulated credit-card
/// provider; otherwise the simulated provider remains active for local development.
/// </summary>
public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    /// <summary>Stripe secret API key (test mode: <c>sk_test_...</c>).</summary>
    public string? ApiKey { get; set; }

    /// <summary>Signing secret used to verify inbound webhook payloads (<c>whsec_...</c>).</summary>
    public string? WebhookSecret { get; set; }
}
