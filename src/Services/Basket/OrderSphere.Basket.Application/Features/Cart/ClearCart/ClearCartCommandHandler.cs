using Microsoft.EntityFrameworkCore;
using OrderSphere.Basket.Application.Abstractions;
using OrderSphere.Basket.Domain.Errors;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Basket.Application.Features.Cart.ClearCart;

public sealed class ClearCartCommandHandler(
    IBasketDbContext context
) : ICommandHandler<ClearCartCommand, Result>
{
    public async Task<Result> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId && !c.IsDeleted, cancellationToken);

        if (cart is null)
            return Result.Failure(CartErrors.CartNotFoundError);

        cart.ClearItems();
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
