using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Abstraction;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Domain.Errors;
using OrderSphere.Ordering.Infrastructure.Persistence;

namespace OrderSphere.Ordering.Api.Features.Order.Admin;

public sealed record UpdateOrderStatusCommand(Guid OrderId, OrderStatus NewStatus) : ICommand<Result>;

public sealed class UpdateOrderStatusCommandHandler(
    IOrderingDbContext context,
    ILogger<UpdateOrderStatusCommandHandler> logger
) : ICommandHandler<UpdateOrderStatusCommand, Result>
{
    public async Task<Result> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await context.Orders
                .FirstOrDefaultAsync(o => o.Id == OrderId.From(request.OrderId) && !o.IsDeleted, cancellationToken);

            if (order is null)
                return Result.Failure(OrderErrors.OrderNotFoundError);

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
                        return Result.Failure(OrderErrors.InvalidStatusTransition);
                    default:
                        return Result.Failure(OrderErrors.InvalidStatusTransition);
                }
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Invalid status transition for order {OrderId} to {NewStatus}",
                    request.OrderId, request.NewStatus);
                return Result.Failure(OrderErrors.InvalidStatusTransition);
            }

            context.Orders.Update(order);
            await context.BeginTransactionAsync(cancellationToken);
            await context.CommitAsync(cancellationToken);

            logger.LogInformation("Order {OrderId} status updated to {NewStatus}", order.Id, request.NewStatus);
            return Result.Success();
        }
        catch (Exception ex)
        {
            await context.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Failed to update status for order {OrderId}", request.OrderId);
            return Result.Failure(OrderErrors.UnknownError);
        }
    }
}
