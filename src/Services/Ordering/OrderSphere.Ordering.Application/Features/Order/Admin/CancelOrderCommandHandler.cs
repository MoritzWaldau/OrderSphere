using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Abstraction;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Domain.Errors;

namespace OrderSphere.Ordering.Application.Features.Order.Admin;

public sealed record CancelOrderCommand(Guid OrderId) : ICommand<Result>;

public sealed class CancelOrderCommandHandler(
    IOrderingDbContext context,
    ICatalogClient catalogClient,
    ILogger<CancelOrderCommandHandler> logger
) : ICommandHandler<CancelOrderCommand, Result>
{
    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await context.BeginTransactionAsync(cancellationToken);

            var order = await context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == OrderId.From(request.OrderId) && !o.IsDeleted, cancellationToken);

            if (order is null)
            {
                await context.RollbackAsync(cancellationToken);
                return Result.Failure(OrderErrors.OrderNotFoundError);
            }

            try { order.Cancel(); }
            catch (InvalidOperationException ex)
            {
                await context.RollbackAsync(cancellationToken);
                logger.LogWarning(ex, "Cannot cancel order {OrderId} in current status", request.OrderId);
                return Result.Failure(OrderErrors.InvalidStatusTransition);
            }

            foreach (var item in order.Items)
            {
                var restoreResult = await catalogClient.RestoreStockAsync(item.ProductId.Value, item.Quantity, cancellationToken);
                if (restoreResult.IsFailure)
                    logger.LogWarning("Stock restore failed for product {ProductId} during order cancellation", item.ProductId);
            }

            context.Orders.Update(order);
            await context.CommitAsync(cancellationToken);

            logger.LogInformation("Order {OrderId} cancelled. Stock restore attempted for {ItemCount} item(s).",
                order.Id, order.Items.Count);

            return Result.Success();
        }
        catch (Exception ex)
        {
            await context.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Failed to cancel order {OrderId}", request.OrderId);
            return Result.Failure(OrderErrors.UnknownError);
        }
    }
}
