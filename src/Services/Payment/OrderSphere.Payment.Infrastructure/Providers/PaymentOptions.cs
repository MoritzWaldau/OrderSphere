namespace OrderSphere.Payment.Infrastructure.Providers;

public sealed class PaymentOptions
{
    public const string SectionName = "Payment";

    /// <summary>
    /// When true, the payment provider step is skipped entirely and every incoming
    /// payment request is immediately marked as captured with a synthetic DEV-* transaction
    /// identifier. Set to true in Development; must be false in Production.
    /// </summary>
    public bool BypassProviders { get; set; }
}
