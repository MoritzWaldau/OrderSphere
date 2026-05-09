using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Order.Admin.CancelOrder;

public sealed class CancelOrderCommandHandler(
    IDbContext context,
    ILogger<CancelOrderCommandHandler> logger
) : ICommandHandler<CancelOrderCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await context.BeginTransactionAsync(cancellationToken);

            var order = await context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId && !o.IsDeleted, cancellationToken);

            if (order is null)
            {
                await context.RollbackAsync(cancellationToken);
                return Result<bool>.Failure(OrderErrors.OrderNotFoundError);
            }

            try
            {
                order.Cancel();
            }
            catch (InvalidOperationException ex)
            {
                await context.RollbackAsync(cancellationToken);
                logger.LogWarning(ex,
                    "Cannot cancel order {OrderId} in current status",
                    request.OrderId);
                return Result<bool>.Failure(OrderErrors.InvalidStatusTransition);
            }

            // Restore stock for each item
            foreach (var item in order.Items)
            {
                var product = await context.Products
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId, cancellationToken);
                if (product is not null)
                {
                    product.AddToStock(item.Quantity);
                    context.Products.Update(product);
                }
            }

            context.Orders.Update(order);
            await context.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Order {OrderId} cancelled. Stock restored for {ItemCount} item(s).",
                order.Id, order.Items.Count);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await context.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Failed to cancel order {OrderId}", request.OrderId);
            return Result<bool>.Failure(OrderErrors.UnknownError);
        }
    }
}
