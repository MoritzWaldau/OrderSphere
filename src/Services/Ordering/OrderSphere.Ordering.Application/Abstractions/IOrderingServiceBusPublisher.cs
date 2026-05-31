using OrderSphere.Ordering.Domain.Events;

namespace OrderSphere.Ordering.Application.Abstractions;

public interface IOrderingServiceBusPublisher
{
    Task PublishCheckoutCartEventAsync(CheckoutCartEvent checkoutCartEvent);
}
