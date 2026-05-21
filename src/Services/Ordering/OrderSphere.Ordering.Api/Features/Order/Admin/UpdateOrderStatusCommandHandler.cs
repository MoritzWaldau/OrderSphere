using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Domain.Errors;
using OrderSphere.Ordering.Infrastructure.Persistence;

namespace OrderSphere.Ordering.Api.Features.Order.Admin;

public sealed record UpdateOrderStatusCommand(Guid OrderId, OrderStatus NewStatus) : IRequest<Result<bool>>;

public sealed class UpdateOrderStatusCommandHandler(
    IOrderingDbContext context,
    ILogger<UpdateOrderStatusCommandHandler> logger
) : IRequestHandler<UpdateOrderStatusCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await context.Orders
                .FirstOrDefaultAsync(o => o.Id == request.OrderId && !o.IsDeleted, cancellationToken);

            if (order is null)
                return Result<bool>.Failure(OrderErrors.OrderNotFoundError);

            try
            {
                switch (request.NewStatus)
                {
                    case OrderStatus.Shipped:
                        order.MarkShipped();
                        break;
                    case OrderStatus.Delivered:
                        order.MarkDelivered();
                        break;
                    case OrderStatus.Cancelled:
                        return Result<bool>.Failure(OrderErrors.InvalidStatusTransition);
                    default:
                        return Result<bool>.Failure(OrderErrors.InvalidStatusTransition);
                }
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Invalid status transition for order {OrderId} to {NewStatus}",
                    request.OrderId, request.NewStatus);
                return Result<bool>.Failure(OrderErrors.InvalidStatusTransition);
            }

            context.Orders.Update(order);
            await context.BeginTransactionAsync(cancellationToken);
            await context.CommitAsync(cancellationToken);

            logger.LogInformation("Order {OrderId} status updated to {NewStatus}", order.Id, request.NewStatus);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await context.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Failed to update status for order {OrderId}", request.OrderId);
            return Result<bool>.Failure(OrderErrors.UnknownError);
        }
    }
}
