using Microsoft.EntityFrameworkCore;
using OrderSphere.Basket.Application.Abstractions;
using OrderSphere.Basket.Domain.Errors;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Basket.Application.Features.Cart.DecreaseCartItem;

public sealed class DecreaseCartItemQuantityCommandHandler(
    IBasketDbContext context
) : ICommandHandler<DecreaseCartItemQuantityCommand, Result>
{
    public async Task<Result> Handle(DecreaseCartItemQuantityCommand request, CancellationToken cancellationToken)
    {
        var cart = await context.Carts
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.CustomerId == request.CustomerId, cancellationToken);

        if (cart is null)
            return Result.Failure(CartErrors.CartNotFoundError);

        var result = cart.DecreaseItem(request.ProductId);
        if (!result.IsSuccess)
            return result;

        if (cart.Items.Count == 0)
            context.Carts.Remove(cart);

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
