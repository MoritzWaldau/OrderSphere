namespace OrderSphere.UserProfile.Domain.Entities;

/// <summary>
/// Notification channel opt-in state for a customer profile.
/// Email is always enabled for transactional messages (order confirmation, refund).
/// SMS and Push default to opt-out per DSGVO; <see cref="ConsentedAt"/> records
/// when the customer explicitly changed their preferences.
/// </summary>
public sealed class NotificationPreferences
{
    public bool EmailEnabled { get; private set; } = true;
    public bool SmsEnabled { get; private set; }
    public bool PushEnabled { get; private set; }
    public DateTime? ConsentedAt { get; private set; }

    private NotificationPreferences() { }

    public static NotificationPreferences Default() => new() { EmailEnabled = true };

    public void Update(bool emailEnabled, bool smsEnabled, bool pushEnabled, DateTime nowUtc)
    {
        EmailEnabled = emailEnabled;
        SmsEnabled = smsEnabled;
        PushEnabled = pushEnabled;
        ConsentedAt = nowUtc;
    }
}
