using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Cart.DecreaseCartItemQuantity;

public sealed class DecreaseCartItemQuantityCommandHandler(
    IDbContext context,
    ILogger<DecreaseCartItemQuantityCommandHandler> logger) 
    : ICommandHandler<DecreaseCartItemQuantityCommand, Result>
{
    public async Task<Result> Handle(DecreaseCartItemQuantityCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await context.BeginTransactionAsync(cancellationToken);
            var cart = await 
                context.Carts
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.CustomerId == request.CustomerId, cancellationToken);

            if (cart is null)
                return Result.Failure(CartErrors.CartNotFoundError);

            var item = cart.Items.FirstOrDefault(x => x.ProductId == request.ProductId);

            if (item is null)
                return Result.Failure(ProductErrors.ProductNotFoundError);

            item.Decrease();

            if (item.Quantity <= 0)
            {
                cart.Items.Remove(item);
            }

            if (cart.Items.Count == 0)
            {
                context.Carts.Remove(cart);
            }

            await context.CommitAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            await context.RollbackAsync(cancellationToken);
            logger.LogError(ex, "An error occurred while decreasing the quantity of a cart item.");
            return Result.Failure(CartErrors.UnknownError);
        }
    }
}
