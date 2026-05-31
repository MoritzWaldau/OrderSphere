using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Webhooks.Domain.Errors;

public static class WebhookErrors
{
    public static readonly Error NotFound = new("Webhook.NotFound", "Webhook subscription was not found.", ErrorType.NotFound);
    public static readonly Error Forbidden = new("Webhook.Forbidden", "Access to this webhook subscription is not permitted.", ErrorType.Forbidden);
}
