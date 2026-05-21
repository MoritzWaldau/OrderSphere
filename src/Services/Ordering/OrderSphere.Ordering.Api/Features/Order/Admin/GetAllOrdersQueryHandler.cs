using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Ordering.Api.Models;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Domain.Errors;
using OrderSphere.Ordering.Infrastructure.Persistence;

namespace OrderSphere.Ordering.Api.Features.Order.Admin;

public sealed record GetAllOrdersQuery(OrderStatus? StatusFilter)
    : IRequest<Result<IReadOnlyList<OrderDto>>>;

public sealed class GetAllOrdersQueryHandler(
    IOrderingDbContext context,
    ILogger<GetAllOrdersQueryHandler> logger
) : IRequestHandler<GetAllOrdersQuery, Result<IReadOnlyList<OrderDto>>>
{
    public async Task<Result<IReadOnlyList<OrderDto>>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = context.Orders.AsNoTracking().Where(o => !o.IsDeleted);
            if (request.StatusFilter is not null)
                query = query.Where(o => o.Status == request.StatusFilter.Value);

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Include(o => o.Items)
                .ToListAsync(cancellationToken);

            var dtos = orders.Select(GetOrdersByCustomerQueryHandler.ToDto).ToList();
            return Result<IReadOnlyList<OrderDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all orders");
            return Result<IReadOnlyList<OrderDto>>.Failure(OrderErrors.UnknownError);
        }
    }
}
