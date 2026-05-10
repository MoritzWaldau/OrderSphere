using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models;
using OrderSphere.Application.Models.Events;
using OrderSphere.Application.ServiceBus;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Entities;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Checkout;

public sealed class CheckoutCartCommandHandler(
    IDbContext context,
    IServiceBusPublisher serviceBusPublisher,
    ILogger<CheckoutCartCommandHandler> logger
    ) : ICommandHandler<CheckoutCartCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await context.BeginTransactionAsync(cancellationToken);

            var cart = await context.Carts
                .AsTracking()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.CustomerId == request.CheckoutCartDto.CustomerId, cancellationToken);

            if (cart is null || cart.CustomerId == Guid.Empty)
            {
                logger.LogError("Cart with customerId {id} was not found in database", request.CheckoutCartDto.CustomerId);
                return Result<Guid>.Failure(CartErrors.CartNotFoundError);
            }

            if (cart.Items.Count == 0)
            {
                logger.LogWarning("Checkout attempted on empty cart for customer {id}", request.CheckoutCartDto.CustomerId);
                return Result<Guid>.Failure(CheckoutCartErrors.EmptyCartError);
            }

            var orderItemDtos = new List<OrderItemDto>();

            foreach (CartItem cartItem in cart.Items)
            {
                var product = await context.Products
                    .FirstOrDefaultAsync(x => x.Id == cartItem.ProductId, cancellationToken);

                if (product is null) continue;

                product.RemoveFromStock(quantity: cartItem.Quantity);
                context.Products.Update(product);

                orderItemDtos.Add(new OrderItemDto(cartItem.ProductId, cartItem.Quantity, product.Price));
            }

            context.CartItems.RemoveRange(cart.Items);

            var correlationId = Guid.CreateVersion7();
            var checkoutCartEvent = new CheckoutCartEvent(
                correlationId,
                request.CheckoutCartDto,
                orderItemDtos);

            await serviceBusPublisher.PublishCheckoutCartEventAsync(checkoutCartEvent);

            await context.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Checkout for customer {CustomerId} accepted. CorrelationId: {CorrelationId}",
                request.CheckoutCartDto.CustomerId,
                correlationId);

            return Result<Guid>.Success(correlationId);
        }
        catch (Exception ex)
        {
            await context.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Checkout failed for customer {id}", request.CheckoutCartDto.CustomerId);

            return Result<Guid>.Failure(CheckoutCartErrors.UnknownError);
        }
    }
}
