using OrderSphere.BuildingBlocks.Contracts.Events;

namespace OrderSphere.Notification.Worker.Email;

public interface INotificationEmailService
{
    Task SendOrderConfirmationAsync(OrderPlacedIntegrationEvent evt, CancellationToken ct = default);
    Task SendInvoiceReadyAsync(InvoiceGeneratedIntegrationEvent evt, CancellationToken ct = default);
}
