using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.Webhooks.Domain.Enums;

namespace OrderSphere.Webhooks.Domain.Entities;

/// <summary>
/// Tracks a single delivery attempt to a webhook subscription endpoint.
/// Each integration event produces one delivery per matching subscription.
/// Failed deliveries are retried with exponential backoff up to <see cref="MaxAttempts"/>.
/// </summary>
public class WebhookDelivery : AuditableEntity
{
    public const int MaxAttempts = 5;

    public Guid SubscriptionId { get; private set; }
    public string EventType { get; private set; } = "";
    public Guid EventId { get; private set; }

    /// <summary>Serialized JSON payload sent in the POST body.</summary>
    public string Payload { get; private set; } = "";

    public DeliveryStatus Status { get; private set; } = DeliveryStatus.Pending;
    public int AttemptCount { get; private set; }
    public int? LastHttpStatus { get; private set; }
    public string? LastError { get; private set; }

    /// <summary>When the next retry is eligible. Null if succeeded or max attempts exhausted.</summary>
    public DateTime? NextRetryAt { get; private set; }

    private WebhookDelivery() { }

    public WebhookDelivery(
        Guid subscriptionId,
        string eventType,
        Guid eventId,
        string payload)
    {
        Id = Guid.NewGuid();
        SubscriptionId = subscriptionId;
        EventType = eventType;
        EventId = eventId;
        Payload = payload;
        Status = DeliveryStatus.Pending;
        AttemptCount = 0;
    }

    public void RecordSuccess(int httpStatus)
    {
        AttemptCount++;
        LastHttpStatus = httpStatus;
        LastError = null;
        Status = DeliveryStatus.Succeeded;
        NextRetryAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordFailure(int? httpStatus, string error)
    {
        AttemptCount++;
        LastHttpStatus = httpStatus;
        LastError = error.Length > 1024 ? error[..1024] : error;
        UpdatedAt = DateTime.UtcNow;

        if (AttemptCount >= MaxAttempts)
        {
            Status = DeliveryStatus.Failed;
            NextRetryAt = null;
        }
        else
        {
            // Exponential backoff: 10s, 40s, 90s, 160s (10 * attempt^2)
            var delaySeconds = 10 * Math.Pow(AttemptCount, 2);
            NextRetryAt = DateTime.UtcNow.AddSeconds(delaySeconds);
        }
    }
}
