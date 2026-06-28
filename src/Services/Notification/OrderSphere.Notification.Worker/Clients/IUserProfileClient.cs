namespace OrderSphere.Notification.Worker.Clients;

public interface IUserProfileClient
{
    /// <summary>
    /// Returns the notification-channel preferences for the customer identified by email.
    /// Returns email-only defaults when the UserProfile service is unavailable or the
    /// profile does not exist (transactional e-mail is always sent).
    /// </summary>
    Task<NotificationPreferences> GetNotificationPreferencesAsync(string customerEmail, CancellationToken ct);
}

public sealed record NotificationPreferences(bool EmailEnabled, bool SmsEnabled, bool PushEnabled)
{
    public static readonly NotificationPreferences Default = new(EmailEnabled: true, SmsEnabled: false, PushEnabled: false);
}
