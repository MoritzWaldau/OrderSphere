using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderSphere.Payment.Domain.Enums;
using OrderSphere.Payment.Infrastructure.Persistence;
using OrderSphere.Payment.Infrastructure.Providers;
using Stripe;

namespace OrderSphere.Payment.Api.Endpoints;

/// <summary>
/// Inbound Stripe webhook. Provides asynchronous capture/refund confirmation: Stripe is the
/// source of truth for settlement, so these events reconcile the local <c>PaymentRecord</c>
/// with the provider. The endpoint is anonymous (authenticated by Stripe signature) and
/// idempotent — a re-delivered event is a no-op once the record reached the target state.
/// </summary>
public static class StripeWebhookEndpoints
{
    private const string PaymentIntentSucceeded = "payment_intent.succeeded";
    private const string PaymentIntentPaymentFailed = "payment_intent.payment_failed";
    private const string ChargeRefunded = "charge.refunded";

    public static void MapStripeWebhookEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/payments/webhooks/stripe", async (
            HttpRequest request,
            PaymentDbContext context,
            IOptions<StripeOptions> stripeOptions,
            ILogger<StripeWebhookMarker> logger,
            CancellationToken ct) =>
        {
            var secret = stripeOptions.Value.WebhookSecret;
            if (string.IsNullOrWhiteSpace(secret))
            {
                logger.LogWarning("Stripe webhook received but no WebhookSecret is configured. Ignoring.");
                return Results.Ok();
            }

            using var reader = new StreamReader(request.Body);
            var json = await reader.ReadToEndAsync(ct);

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, request.Headers["Stripe-Signature"], secret);
            }
            catch (StripeException ex)
            {
                logger.LogWarning(ex, "Stripe webhook signature verification failed.");
                return Results.BadRequest();
            }

            // payment_intent.* carry a PaymentIntent; charge.refunded carries a Charge that
            // references the originating PaymentIntent. Both resolve to a PaymentIntent id.
            var paymentIntentId = stripeEvent.Data.Object switch
            {
                PaymentIntent intent => intent.Id,
                Charge charge => charge.PaymentIntentId,
                _ => null
            };

            if (paymentIntentId is null)
            {
                logger.LogInformation("Stripe webhook {Type} ignored (no PaymentIntent reference).", stripeEvent.Type);
                return Results.Ok();
            }

            return await ReconcileAsync(context, logger, paymentIntentId, stripeEvent.Type, ct);
        });
    }

    private static async Task<IResult> ReconcileAsync(
        PaymentDbContext context,
        ILogger logger,
        string paymentIntentId,
        string eventType,
        CancellationToken ct)
    {
        var record = await context.Payments.FirstOrDefaultAsync(p => p.TransactionId == paymentIntentId, ct);
        if (record is null)
        {
            logger.LogWarning("Stripe webhook {Type}: no PaymentRecord for intent {IntentId}.", eventType, paymentIntentId);
            return Results.Ok();
        }

        switch (eventType)
        {
            case PaymentIntentSucceeded when record.Status != PaymentStatus.Captured:
                record.MarkCaptured(paymentIntentId);
                break;
            case PaymentIntentPaymentFailed when record.Status is not (PaymentStatus.Failed or PaymentStatus.Captured):
                record.MarkFailed("Stripe reported payment failure.");
                break;
            case ChargeRefunded when record.Status != PaymentStatus.Refunded:
                record.MarkRefunded();
                break;
            default:
                // Already reconciled or an event we don't act on.
                return Results.Ok();
        }

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Stripe webhook {Type} reconciled PaymentRecord for intent {IntentId}.",
            eventType, paymentIntentId);
        return Results.Ok();
    }

    /// <summary>Logger category marker for the webhook endpoint.</summary>
    public sealed class StripeWebhookMarker;
}
