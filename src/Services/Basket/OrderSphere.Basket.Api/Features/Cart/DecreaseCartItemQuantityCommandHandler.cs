using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSphere.Basket.Domain.Errors;
using OrderSphere.Basket.Infrastructure.Persistence;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Basket.Api.Features.Cart;

public sealed class DecreaseCartItemQuantityCommandHandler(
    BasketDbContext context,
    ILogger<DecreaseCartItemQuantityCommandHandler> logger
) : IRequestHandler<DecreaseCartItemQuantityCommand, Result>
{
    public async Task<Result> Handle(DecreaseCartItemQuantityCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var cart = await context.Carts
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.CustomerId == request.CustomerId, cancellationToken);

            if (cart is null)
                return Result.Failure(CartErrors.CartNotFoundError);

            var item = cart.Items.FirstOrDefault(x => x.ProductId == request.ProductId);
            if (item is null)
                return Result.Failure(ProductErrors.ProductNotFoundError);

            item.Decrease();

            if (item.Quantity <= 0)
                cart.Items.Remove(item);

            if (cart.Items.Count == 0)
                context.Carts.Remove(cart);

            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error decreasing cart item quantity for customer {CustomerId}", request.CustomerId);
            return Result.Failure(CartErrors.UnknownError);
        }
    }
}
