using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Domain.Primitives;
using OrderSphere.Ordering.Domain.Errors;
using OrderSphere.Ordering.Infrastructure.Persistence;

namespace OrderSphere.Ordering.Api.Features.Cart;

public sealed class RemoveFromCartCommandHandler(
    IOrderingDbContext context,
    ILogger<RemoveFromCartCommandHandler> logger
) : IRequestHandler<RemoveFromCartCommand, Result>
{
    public async Task<Result> Handle(RemoveFromCartCommand request, CancellationToken cancellationToken)
    {
        await context.BeginTransactionAsync(cancellationToken);

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

            cart.Items.Remove(item);

            if (cart.Items.Count == 0)
                context.Carts.Remove(cart);

            await context.CommitAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            await context.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error removing product {ProductId} from cart for customer {CustomerId}",
                request.ProductId, request.CustomerId);
            return Result.Failure(CartErrors.UnknownError);
        }
    }
}
