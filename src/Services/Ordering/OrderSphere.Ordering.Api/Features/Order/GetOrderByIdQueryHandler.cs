using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Domain.Primitives;
using OrderSphere.Ordering.Api.Models;
using OrderSphere.Ordering.Domain.Errors;
using OrderSphere.Ordering.Infrastructure.Persistence;

namespace OrderSphere.Ordering.Api.Features.Order;

public sealed record GetOrderByIdQuery(Guid OrderId, Guid CustomerId)
    : IRequest<Result<OrderDto>>;

public sealed class GetOrderByIdQueryHandler(
    IOrderingDbContext context,
    ILogger<GetOrderByIdQueryHandler> logger
) : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId && !o.IsDeleted, cancellationToken);

            if (order is null)
                return Result<OrderDto>.Failure(OrderErrors.OrderNotFoundError);

            if (order.CustomerId != request.CustomerId)
            {
                logger.LogWarning("Customer {CustomerId} attempted to access foreign order {OrderId}",
                    request.CustomerId, request.OrderId);
                return Result<OrderDto>.Failure(OrderErrors.OrderNotFoundError);
            }

            return Result<OrderDto>.Success(GetOrdersByCustomerQueryHandler.ToDto(order));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving order {OrderId} for customer {CustomerId}",
                request.OrderId, request.CustomerId);
            return Result<OrderDto>.Failure(OrderErrors.UnknownError);
        }
    }
}
