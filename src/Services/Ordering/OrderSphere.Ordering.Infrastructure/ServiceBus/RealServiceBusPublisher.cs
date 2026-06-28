using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.BuildingBlocks.EventBus;
using OrderSphere.Ordering.Domain.Events;

namespace OrderSphere.Ordering.Infrastructure.ServiceBus;

public sealed class RealServiceBusPublisher(IEventBus eventBus)
{
    private const string QueueName = "orders";

    public async Task PublishCheckoutCartEventAsync(CheckoutCartEvent checkoutCartEvent)
    {
        var integrationEvent = new CheckoutCartIntegrationEvent
        {
            CorrelationId = checkoutCartEvent.CorrelationId,
            CustomerId = checkoutCartEvent.CheckoutCart.CustomerId,
            CustomerEmail = checkoutCartEvent.CheckoutCart.CustomerEmail,
            CustomerName = checkoutCartEvent.CheckoutCart.CustomerName,
            ShippingAddress = new ShippingAddressDto(
                checkoutCartEvent.CheckoutCart.ShippingAddress.FirstName,
                checkoutCartEvent.CheckoutCart.ShippingAddress.LastName,
                checkoutCartEvent.CheckoutCart.ShippingAddress.Street,
                checkoutCartEvent.CheckoutCart.ShippingAddress.City,
                checkoutCartEvent.CheckoutCart.ShippingAddress.PostalCode,
                checkoutCartEvent.CheckoutCart.ShippingAddress.Country),
            PaymentMethod = checkoutCartEvent.CheckoutCart.PaymentMethod.ToString(),
            CouponCode = checkoutCartEvent.CheckoutCart.CouponCode,
            Items = checkoutCartEvent.Items.Select(i => new OrderItemDto(
                i.ProductId, i.ProductName, i.Quantity, i.Price, i.CategoryId)).ToList()
        };

        await eventBus.PublishAsync(integrationEvent, QueueName);
    }
}
