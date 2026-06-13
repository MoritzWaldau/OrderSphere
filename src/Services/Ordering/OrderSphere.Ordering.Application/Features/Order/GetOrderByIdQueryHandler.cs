using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Application.Models;
using OrderSphere.Ordering.Domain.Errors;

namespace OrderSphere.Ordering.Application.Features.Order;

/// <summary>
/// Loads a single order by its ID without a customer-ownership filter.
/// Caller identity and ownership are enforced at the endpoint via
/// <c>IAuthorizationService.AuthorizeAsync(user, orderDto, "OrderOwnerOrStaff")</c>.
/// </summary>
public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery<Result<OrderDto>>;

public sealed class GetOrderByIdQueryHandler(
    IOrderingDbContext context,
    ILogger<GetOrderByIdQueryHandler> logger
) : IQueryHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == OrderId.From(request.OrderId), cancellationToken);

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
            logger.LogError(ex, "Error retrieving order {OrderId}", request.OrderId);
            return Result<OrderDto>.Failure(OrderErrors.UnknownError);
        }
    }
}
