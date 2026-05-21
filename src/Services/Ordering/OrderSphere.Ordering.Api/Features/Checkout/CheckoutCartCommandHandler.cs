using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Ordering.Api.Abstractions;
using OrderSphere.Ordering.Domain.Entities;
using OrderSphere.Ordering.Domain.Errors;
using OrderSphere.Ordering.Domain.Events;
using OrderSphere.Ordering.Infrastructure.Outbox;
using OrderSphere.Ordering.Infrastructure.Persistence;

namespace OrderSphere.Ordering.Api.Features.Checkout;

public sealed class CheckoutCartCommandHandler(
    IOrderingDbContext context,
    ICatalogClient catalogClient,
    IOrderingServiceBusPublisher serviceBusPublisher,
    ILogger<CheckoutCartCommandHandler> logger
) : IRequestHandler<CheckoutCartCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await context.BeginTransactionAsync(cancellationToken);

            var req = request.Request;

            var cart = await context.Carts
                .AsTracking()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.CustomerId == req.CustomerId, cancellationToken);

            if (cart is null || cart.CustomerId == Guid.Empty)
            {
                logger.LogError("Cart with customerId {Id} was not found", req.CustomerId);
                return Result<Guid>.Failure(CartErrors.CartNotFoundError);
            }

            if (cart.Items.Count == 0)
            {
                logger.LogWarning("Checkout attempted on empty cart for customer {Id}", req.CustomerId);
                return Result<Guid>.Failure(CheckoutCartErrors.EmptyCartError);
            }

            var orderItemDtos = new List<OrderItemEventDto>();

            foreach (CartItem cartItem in cart.Items)
            {
                var productResult = await catalogClient.GetProductByIdAsync(cartItem.ProductId, cancellationToken);
                if (productResult.IsFailure) continue;

                var decrementResult = await catalogClient.DecrementStockAsync(
                    cartItem.ProductId, cartItem.Quantity, cancellationToken);

                if (decrementResult.IsFailure)
                {
                    await context.RollbackAsync(cancellationToken);
                    logger.LogWarning("Stock decrement failed for product {ProductId}", cartItem.ProductId);
                    return Result<Guid>.Failure(ProductErrors.InsufficientStockError);
                }

                orderItemDtos.Add(new OrderItemEventDto(
                    cartItem.ProductId,
                    productResult.Value.Name,
                    cartItem.Quantity,
                    productResult.Value.Price));
            }

            context.CartItems.RemoveRange(cart.Items);

            var correlationId = Guid.CreateVersion7();
            var checkoutCartEvent = new CheckoutCartEvent(
                correlationId,
                new CheckoutCartDto(
                    req.CustomerId,
                    req.CustomerEmail,
                    req.CustomerName,
                    req.ShippingAddress,
                    req.PaymentMethod),
                orderItemDtos);

            await serviceBusPublisher.PublishCheckoutCartEventAsync(checkoutCartEvent);
            await context.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Checkout for customer {CustomerId} accepted. CorrelationId: {CorrelationId}",
                req.CustomerId, correlationId);

            return Result<Guid>.Success(correlationId);
        }
        catch (Exception ex)
        {
            await context.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Checkout failed for customer {Id}", request.Request.CustomerId);
            return Result<Guid>.Failure(CheckoutCartErrors.UnknownError);
        }
    }
}
