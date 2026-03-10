using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Cart.UpdateCart;

public sealed class UpdateCartCommandHandler(IDbContext context, ILogger<UpdateCartCommandHandler> logger) : ICommandHandler<UpdateCartCommand, Result>
{
    public async Task<Result> Handle(UpdateCartCommand request, CancellationToken cancellationToken)
    {
        await context.BeginTransactionAsync(cancellationToken);

        try
        {
            var cart = await context.Carts.FirstOrDefaultAsync(x => x.Id == request.Cart.CustomerId, cancellationToken) 
                ?? new Domain.Entities.Cart(request.Cart.CustomerId);

            foreach (var item in request.Cart.Items)
            {
                cart.AddItem(item.ProductId, item.Quantity);
            }

            await context.CommitAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            await context.RollbackAsync(cancellationToken);
            logger.LogError(ex, "An error occurred while creating the cart for customer {CustomerId}", request.Cart.CustomerId);
            return Result.Failure(CartErrors.UnknownError);
        }
    }
}
