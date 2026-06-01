using OrderSphere.BuildingBlocks.Contracts.Events;

namespace OrderSphere.Notification.Worker.Email;

internal sealed class LoggingNotificationEmailService(ILogger<LoggingNotificationEmailService> logger)
    : INotificationEmailService
{
    public Task SendOrderConfirmationAsync(OrderPlacedIntegrationEvent evt, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[DEV] Order confirmation email suppressed. OrderId={OrderId} Tracking={TrackingNumber} To={Email}",
            evt.OrderId, evt.TrackingNumber, evt.CustomerEmail);
        return Task.CompletedTask;
    }
}
