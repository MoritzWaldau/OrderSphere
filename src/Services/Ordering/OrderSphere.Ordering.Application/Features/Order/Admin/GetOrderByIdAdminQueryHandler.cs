using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Abstraction;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Application.Models;
using OrderSphere.Ordering.Domain.Errors;
using OrderSphere.Ordering.Application.Abstractions;

namespace OrderSphere.Ordering.Application.Features.Order.Admin;

public sealed record GetOrderByIdAdminQuery(Guid OrderId) : IQuery<Result<OrderDto>>;

public sealed class GetOrderByIdAdminQueryHandler(
    IOrderingDbContext context,
    ILogger<GetOrderByIdAdminQueryHandler> logger
) : IQueryHandler<GetOrderByIdAdminQuery, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(GetOrderByIdAdminQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == OrderId.From(request.OrderId) && !o.IsDeleted, cancellationToken);

            if (order is null)
                return Result<OrderDto>.Failure(OrderErrors.OrderNotFoundError);

            return Result<OrderDto>.Success(GetOrdersByCustomerQueryHandler.ToDto(order));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving admin order {OrderId}", request.OrderId);
            return Result<OrderDto>.Failure(OrderErrors.UnknownError);
        }
    }
}
