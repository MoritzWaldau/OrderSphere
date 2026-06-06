using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Application.Models;
using OrderSphere.Ordering.Domain.Errors;

namespace OrderSphere.Ordering.Application.Features.Order;

/// <summary>
/// Loads an order by its Service Bus correlation ID without a customer filter.
/// Ownership is enforced at the endpoint via
/// <c>IAuthorizationService.AuthorizeAsync(user, orderDto, "OrderOwnerOrStaff")</c>.
/// Returns <c>null</c> in the success value when the order has not yet been
/// persisted (normal during Service Bus processing delay).
/// </summary>
public sealed record GetOrderByCorrelationIdQuery(Guid CorrelationId)
    : IQuery<Result<OrderDto?>>;

public sealed class GetOrderByCorrelationIdQueryHandler(
    IOrderingDbContext context,
    ILogger<GetOrderByCorrelationIdQueryHandler> logger
) : IQueryHandler<GetOrderByCorrelationIdQuery, Result<OrderDto?>>
{
    public async Task<Result<OrderDto?>> Handle(GetOrderByCorrelationIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.CorrelationId == request.CorrelationId && !o.IsDeleted, cancellationToken);

            if (order is null)
                return Result<OrderDto?>.Success(null);

            return Result<OrderDto?>.Success(GetOrdersByCustomerQueryHandler.ToDto(order));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving order by correlationId {CorrelationId}", request.CorrelationId);
            return Result<OrderDto?>.Failure(OrderErrors.UnknownError);
        }
    }
}
