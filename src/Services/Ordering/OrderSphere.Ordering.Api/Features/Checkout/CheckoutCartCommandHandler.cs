using MediatR;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Ordering.Api.Abstractions;
using OrderSphere.Ordering.Domain.Errors;
using OrderSphere.Ordering.Domain.Events;
using OrderSphere.Ordering.Infrastructure.Outbox;

namespace OrderSphere.Ordering.Api.Features.Checkout;

public sealed class CheckoutCartCommandHandler(
    ICatalogClient catalogClient,
    IBasketClient basketClient,
    IOrderingServiceBusPublisher serviceBusPublisher,
    ILogger<CheckoutCartCommandHandler> logger
) : IRequestHandler<CheckoutCartCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var cartResult = await basketClient.GetCartAsync(request.CustomerId, cancellationToken);
            if (cartResult.IsFailure)
            {
                logger.LogError("Cart not found for customer {Id} via Basket service", request.CustomerId);
                return Result<Guid>.Failure(CheckoutCartErrors.CartNotFoundError);
            }

            var cart = cartResult.Value;
            if (cart.Items.Count == 0)
            {
                logger.LogWarning("Checkout attempted on empty cart for customer {Id}", request.CustomerId);
                return Result<Guid>.Failure(CheckoutCartErrors.EmptyCartError);
            }

            var orderItemDtos = new List<OrderItemEventDto>();

            foreach (var cartItem in cart.Items)
            {
                var productResult = await catalogClient.GetProductByIdAsync(cartItem.ProductId, cancellationToken);
                if (productResult.IsFailure) continue;

                var decrementResult = await catalogClient.DecrementStockAsync(
                    cartItem.ProductId, cartItem.Quantity, cancellationToken);

                if (decrementResult.IsFailure)
                {
                    logger.LogWarning("Stock decrement failed for product {ProductId}", cartItem.ProductId);
                    return Result<Guid>.Failure(ProductErrors.InsufficientStockError);
                }

                orderItemDtos.Add(new OrderItemEventDto(
                    cartItem.ProductId,
                    productResult.Value.Name,
                    cartItem.Quantity,
                    productResult.Value.Price));
            }

            var correlationId = Guid.CreateVersion7();
            var checkoutCartEvent = new CheckoutCartEvent(
                correlationId,
                new CheckoutCartDto(
                    request.CustomerId,
                    request.CustomerEmail,
                    request.CustomerName,
                    request.ShippingAddress,
                    request.PaymentMethod),
                orderItemDtos);

            await serviceBusPublisher.PublishCheckoutCartEventAsync(checkoutCartEvent);

            await basketClient.ClearCartItemsAsync(request.CustomerId, cancellationToken);

            logger.LogInformation(
                "Checkout for customer {CustomerId} accepted. CorrelationId: {CorrelationId}",
                request.CustomerId, correlationId);

            return Result<Guid>.Success(correlationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Checkout failed for customer {Id}", request.CustomerId);
            return Result<Guid>.Failure(CheckoutCartErrors.UnknownError);
        }
    }
}
