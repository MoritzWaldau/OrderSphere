using OrderSphere.BuildingBlocks.Contracts.Events;

namespace OrderSphere.Notification.Worker.Channels;

/// <summary>
/// Strategy interface for a single notification delivery channel (e-mail, SMS, push).
/// Each implementation decides whether it can deliver the message and how.
/// </summary>
public interface INotificationChannel
{
    NotificationChannelType ChannelType { get; }
    Task SendOrderConfirmationAsync(OrderPlacedIntegrationEvent evt, CancellationToken ct);
}

public enum NotificationChannelType { Email, Sms, Push }
