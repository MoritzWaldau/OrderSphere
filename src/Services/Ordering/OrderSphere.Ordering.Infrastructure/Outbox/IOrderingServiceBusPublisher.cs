using OrderSphere.Ordering.Domain.Events;

namespace OrderSphere.Ordering.Infrastructure.Outbox;

public interface IOrderingServiceBusPublisher
{
    Task PublishCheckoutCartEventAsync(CheckoutCartEvent checkoutCartEvent);
}
