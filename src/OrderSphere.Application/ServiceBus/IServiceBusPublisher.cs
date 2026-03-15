using OrderSphere.Application.Models.Events;

namespace OrderSphere.Application.ServiceBus;

public interface IServiceBusPublisher
{
    Task PublishCheckoutCartEventAsync(CheckoutCartEvent checkoutCartEvent);
}
