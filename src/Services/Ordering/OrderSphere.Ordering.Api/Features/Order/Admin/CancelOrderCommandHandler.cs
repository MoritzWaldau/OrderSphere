using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Domain.Primitives;
using OrderSphere.Ordering.Api.Abstractions;
using OrderSphere.Ordering.Domain.Errors;
using OrderSphere.Ordering.Infrastructure.Persistence;

namespace OrderSphere.Ordering.Api.Features.Order.Admin;

public sealed record CancelOrderCommand(Guid OrderId) : IRequest<Result<bool>>;

public sealed class CancelOrderCommandHandler(
    IOrderingDbContext context,
    ICatalogClient catalogClient,
    ILogger<CancelOrderCommandHandler> logger
) : IRequestHandler<CancelOrderCommand, Result<bool>>
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

            try { order.Cancel(); }
            catch (InvalidOperationException ex)
            {
                await context.RollbackAsync(cancellationToken);
                logger.LogWarning(ex, "Cannot cancel order {OrderId} in current status", request.OrderId);
                return Result<bool>.Failure(OrderErrors.InvalidStatusTransition);
            }

            foreach (var item in order.Items)
            {
                var restoreResult = await catalogClient.RestoreStockAsync(item.ProductId, item.Quantity, cancellationToken);
                if (restoreResult.IsFailure)
                    logger.LogWarning("Stock restore failed for product {ProductId} during order cancellation", item.ProductId);
            }

            context.Orders.Update(order);
            await context.CommitAsync(cancellationToken);

            logger.LogInformation("Order {OrderId} cancelled. Stock restore attempted for {ItemCount} item(s).",
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
