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

            var orderItems = new List<OrderItem>();
            var orderItemDtos = new List<OrderItemDto>();

            foreach (CartItem cartItem in cart.Items)
            {
                var product = await context.Products
                    .FirstOrDefaultAsync(x => x.Id == cartItem.ProductId, cancellationToken);

                if (product is null) continue;

                product.RemoveFromStock(quantity: cartItem.Quantity);
                context.Products.Update(product);

                orderItems.Add(new OrderItem(cartItem.ProductId, cartItem.Quantity, product.Price));
                orderItemDtos.Add(new OrderItemDto(cartItem.ProductId, cartItem.Quantity, product.Price));
            }

            var order = new Domain.Entities.Order(
                request.CheckoutCartDto.CustomerId,
                request.CheckoutCartDto.ShippingAddress,
                request.CheckoutCartDto.PaymentMethod,
                orderItems);

            await context.Orders.AddAsync(order, cancellationToken);

            context.CartItems.RemoveRange(cart.Items);

            await context.CommitAsync(cancellationToken);

            try
            {
                var checkoutCartEvent = new CheckoutCartEvent(
                    request.CheckoutCartDto,
                    orderItemDtos);

                await serviceBusPublisher.PublishCheckoutCartEventAsync(checkoutCartEvent);
            }
            catch (Exception publishEx)
            {
                logger.LogWarning(publishEx,
                    "Order {OrderId} was created but publishing CheckoutCartEvent failed. Downstream tasks (e.g. confirmation email) will not run.",
                    order.Id);
            }

            return Result<Guid>.Success(order.Id);
        }
        catch (Exception ex)
        {
            await context.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Checkout failed for customer {id}", request.CheckoutCartDto.CustomerId);

            return Result<Guid>.Failure(CheckoutCartErrors.UnknownError);
        }
    }
}
