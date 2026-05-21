using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Domain.Primitives;
using OrderSphere.Ordering.Api.Models;
using OrderSphere.Ordering.Domain.Errors;
using OrderSphere.Ordering.Infrastructure.Persistence;

namespace OrderSphere.Ordering.Api.Features.Order;

public sealed record GetOrderByCorrelationIdQuery(Guid CorrelationId, Guid CustomerId)
    : IRequest<Result<OrderDto?>>;

public sealed class GetOrderByCorrelationIdQueryHandler(
    IOrderingDbContext context,
    ILogger<GetOrderByCorrelationIdQueryHandler> logger
) : IRequestHandler<GetOrderByCorrelationIdQuery, Result<OrderDto?>>
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

            if (order.CustomerId != request.CustomerId)
            {
                logger.LogWarning("Customer {CustomerId} attempted to access order with foreign correlationId {CorrelationId}",
                    request.CustomerId, request.CorrelationId);
                return Result<OrderDto?>.Success(null);
            }

            return Result<OrderDto?>.Success(GetOrdersByCustomerQueryHandler.ToDto(order));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving order by correlationId {CorrelationId}", request.CorrelationId);
            return Result<OrderDto?>.Failure(OrderErrors.UnknownError);
        }
    }
}
