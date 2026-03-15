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
    ) : ICommandHandler<CheckoutCartCommand, Result>
{
    public async Task<Result> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await context.BeginTransactionAsync(cancellationToken);
            var cart = await context.Carts.Include(x => x.Items).FirstOrDefaultAsync(x => x.CustomerId == request.CheckoutCartDto.CustomerId, cancellationToken);

            if(cart is null || cart.CustomerId == Guid.Empty)
            {
                logger.LogError("Cart with customerId {id} was not found in database", request.CheckoutCartDto.CustomerId);
                return Result.Failure(CartErrors.CartNotFoundError);
            }

            var orderItems = new List<OrderItemDto>();

            foreach (CartItem cartItem in cart.Items)
            {
                var product = await context.Products.FirstOrDefaultAsync(x => x.Id == cartItem.ProductId, cancellationToken);
                if(product is not null)
                {
                    product.RemoveFromStock(quantity: cartItem.Quantity);
                    context.Products.Update(product);
                    var orderItem = new OrderItemDto(cartItem.ProductId, cartItem.Quantity, product.Price);
                    orderItems.Add(orderItem);
                }
            }

            var checkoutCartEvent = new CheckoutCartEvent(
                request.CheckoutCartDto,
                orderItems
                );

            await serviceBusPublisher.PublishCheckoutCartEventAsync(checkoutCartEvent);

            await context.CommitAsync(cancellationToken);
            return Result.Success();
        }
        catch(Exception ex) 
        {
            await context.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Something went wrong");

            return Result.Failure(CheckoutCartErrors.UnknownError);
        }
    }
}
