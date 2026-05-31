using Microsoft.EntityFrameworkCore;
using OrderSphere.Basket.Application.Abstractions;
using OrderSphere.Basket.Domain.Errors;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Basket.Application.Features.Cart.RemoveFromCart;

public sealed class RemoveFromCartCommandHandler(
    IBasketDbContext context
) : ICommandHandler<RemoveFromCartCommand, Result>
{
    public async Task<Result> Handle(RemoveFromCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await context.Carts
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.CustomerId == request.CustomerId, cancellationToken);

        if (cart is null)
            return Result.Failure(CartErrors.CartNotFoundError);

        var result = cart.RemoveItem(request.ProductId);
        if (!result.IsSuccess)
            return result;

        if (cart.Items.Count == 0)
            context.Carts.Remove(cart);

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
