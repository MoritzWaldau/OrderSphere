using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.Webhooks.Domain.Enums;

namespace OrderSphere.Webhooks.Domain.Entities;

/// <summary>
/// A registered endpoint that receives webhook deliveries for selected event types.
/// Each subscription belongs to a specific customer (identified by Keycloak sub claim).
/// </summary>
public class WebhookSubscription : AuditableEntity
{
    /// <summary>Customer who owns this subscription (Keycloak sub).</summary>
    public Guid CustomerId { get; private set; }

    /// <summary>Target URL that receives POST deliveries.</summary>
    public string Url { get; private set; } = "";

    /// <summary>HMAC-SHA256 secret used to sign the delivery payload.</summary>
    public string Secret { get; private set; } = "";

    /// <summary>Comma-separated list of subscribed event types.</summary>
    public string Events { get; private set; } = "";

    /// <summary>Whether this subscription is currently active.</summary>
    public bool IsActive { get; private set; } = true;

    private WebhookSubscription() { }

    public WebhookSubscription(
        Guid customerId,
        string url,
        string secret,
        IEnumerable<WebhookEventType> events)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        Url = url;
        Secret = secret;
        Events = string.Join(",", events.Select(e => e.ToString()));
        IsActive = true;
    }

    public void Update(string url, string secret, IEnumerable<WebhookEventType> events)
    {
        Url = url;
        Secret = secret;
        Events = string.Join(",", events.Select(e => e.ToString()));
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public IReadOnlyList<WebhookEventType> GetEventTypes()
    {
        if (string.IsNullOrEmpty(Events)) return [];

        return Events.Split(',')
            .Select(e => Enum.Parse<WebhookEventType>(e.Trim()))
            .ToList();
    }

    public bool ListensTo(WebhookEventType eventType)
        => GetEventTypes().Contains(eventType);
}
