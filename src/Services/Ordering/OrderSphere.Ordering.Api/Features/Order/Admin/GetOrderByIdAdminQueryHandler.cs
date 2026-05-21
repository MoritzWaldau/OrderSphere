using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Domain.Primitives;
using OrderSphere.Ordering.Api.Models;
using OrderSphere.Ordering.Domain.Errors;
using OrderSphere.Ordering.Infrastructure.Persistence;

namespace OrderSphere.Ordering.Api.Features.Order.Admin;

public sealed record GetOrderByIdAdminQuery(Guid OrderId) : IRequest<Result<OrderDto>>;

public sealed class GetOrderByIdAdminQueryHandler(
    IOrderingDbContext context,
    ILogger<GetOrderByIdAdminQueryHandler> logger
) : IRequestHandler<GetOrderByIdAdminQuery, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(GetOrderByIdAdminQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId && !o.IsDeleted, cancellationToken);

            if (order is null)
                return Result<OrderDto>.Failure(OrderErrors.OrderNotFoundError);

            return Result<OrderDto>.Success(GetOrdersByCustomerQueryHandler.ToDto(order));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving admin order {OrderId}", request.OrderId);
            return Result<OrderDto>.Failure(OrderErrors.UnknownError);
        }
    }
}
