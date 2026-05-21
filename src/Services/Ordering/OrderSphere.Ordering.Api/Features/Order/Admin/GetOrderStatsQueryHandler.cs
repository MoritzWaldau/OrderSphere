using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Ordering.Api.Models;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Domain.Errors;
using OrderSphere.Ordering.Infrastructure.Persistence;

namespace OrderSphere.Ordering.Api.Features.Order.Admin;

public sealed record GetOrderStatsQuery : IRequest<Result<OrderStatsDto>>;

public sealed class GetOrderStatsQueryHandler(
    IOrderingDbContext context,
    ILogger<GetOrderStatsQueryHandler> logger
) : IRequestHandler<GetOrderStatsQuery, Result<OrderStatsDto>>
{
    public async Task<Result<OrderStatsDto>> Handle(GetOrderStatsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var todayStart = DateTime.UtcNow.Date;

            var allOrders = await context.Orders
                .AsNoTracking()
                .Where(o => !o.IsDeleted)
                .Include(o => o.Items)
                .ToListAsync(cancellationToken);

            var nonCancelled = allOrders.Where(o => o.Status != OrderStatus.Cancelled).ToList();

            var totalOrders = allOrders.Count;
            var ordersToday = allOrders.Count(o => o.CreatedAt >= todayStart);
            var totalRevenue = nonCancelled.Sum(o => o.Items.Sum(i => i.Price * i.Quantity));
            var revenueToday = nonCancelled
                .Where(o => o.CreatedAt >= todayStart)
                .Sum(o => o.Items.Sum(i => i.Price * i.Quantity));
            var pendingShipments = allOrders.Count(o => o.Status == OrderStatus.Paid);

            var totalCustomers = await context.Orders
                .AsNoTracking()
                .Where(o => !o.IsDeleted)
                .Select(o => o.CustomerId)
                .Distinct()
                .CountAsync(cancellationToken);

            var recent = allOrders
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new RecentOrderDto(
                    o.Id,
                    $"{o.ShippingAddress.FirstName} {o.ShippingAddress.LastName}",
                    o.Items.Sum(i => i.Price * i.Quantity),
                    o.Status.ToString(),
                    o.CreatedAt))
                .ToList();

            return Result<OrderStatsDto>.Success(new OrderStatsDto(
                totalOrders, ordersToday, totalRevenue, revenueToday,
                pendingShipments, totalCustomers, recent));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error building order stats");
            return Result<OrderStatsDto>.Failure(OrderErrors.UnknownError);
        }
    }
}
