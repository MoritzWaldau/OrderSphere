namespace OrderSphere.Notification.Worker.Clients;

/// <summary>
/// Used when no UserProfile service URL is configured (local dev without full stack).
/// Always returns email-only defaults so order confirmations still reach customers.
/// </summary>
public sealed class FallbackUserProfileClient(ILogger<FallbackUserProfileClient> logger) : IUserProfileClient
{
    public Task<NotificationPreferences> GetNotificationPreferencesAsync(string customerEmail, CancellationToken ct)
    {
        logger.LogDebug("UserProfile URL not configured; using default notification preferences for {Email}.", customerEmail);
        return Task.FromResult(NotificationPreferences.Default);
    }
}
