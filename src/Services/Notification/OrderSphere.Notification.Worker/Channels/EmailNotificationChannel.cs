using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.Notification.Worker.Email;

namespace OrderSphere.Notification.Worker.Channels;

/// <summary>
/// Notification channel that delivers order confirmation via e-mail.
/// Delegates to <see cref="INotificationEmailService"/> which either sends via
/// Azure Communication Services or logs (development fallback).
/// </summary>
public sealed class EmailNotificationChannel(INotificationEmailService emailService) : INotificationChannel
{
    public NotificationChannelType ChannelType => NotificationChannelType.Email;

    public Task SendOrderConfirmationAsync(OrderPlacedIntegrationEvent evt, CancellationToken ct)
        => emailService.SendOrderConfirmationAsync(evt, ct);
}
