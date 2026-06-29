using OrderSphere.BuildingBlocks.Contracts.Events;

namespace OrderSphere.Notification.Worker.Email;

internal sealed class LoggingNotificationEmailService(ILogger<LoggingNotificationEmailService> logger)
    : INotificationEmailService
{
    public Task SendOrderConfirmationAsync(OrderPlacedIntegrationEvent evt, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[DEV] Order confirmation email suppressed. OrderId={OrderId} Tracking={TrackingNumber} To={Email}",
            evt.OrderId, evt.TrackingNumber, MaskEmail(evt.CustomerEmail));
        return Task.CompletedTask;
    }

    public Task SendInvoiceReadyAsync(InvoiceGeneratedIntegrationEvent evt, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[DEV] Invoice-ready email suppressed. Invoice={InvoiceNumber} OrderId={OrderId} To={Email}",
            evt.InvoiceNumber, evt.OrderId, MaskEmail(evt.CustomerEmail));
        return Task.CompletedTask;
    }

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "(none)";

        var at = email.IndexOf('@');
        return at <= 0 ? "***" : $"{email[0]}***{email[at..]}";
    }
}
