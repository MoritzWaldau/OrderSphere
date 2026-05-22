using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models.Admin;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Admin.GetDashboardStats;

public sealed class GetDashboardStatsQueryHandler(
    IOrderingClient orderingClient,
    ILogger<GetDashboardStatsQueryHandler> logger
) : IQueryHandler<GetDashboardStatsQuery, Result<AdminDashboardDto>>
{
    public async Task<Result<AdminDashboardDto>> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var statsResult = await orderingClient.GetOrderStatsAsync(cancellationToken);
            if (statsResult.IsFailure)
                return Result<AdminDashboardDto>.Failure(statsResult.Error);

            var s = statsResult.Value;

            var recent = s.RecentOrders
                .Select(r => new RecentOrderDto(r.Id, r.CustomerName, r.Total, r.Status, r.CreatedAt))
                .ToList();

            var dto = new AdminDashboardDto(
                s.TotalOrders,
                s.OrdersToday,
                s.TotalRevenue,
                s.RevenueToday,
                s.PendingShipments,
                LowStockProductsCount: 0,
                TotalProducts: 0,
                s.TotalCustomers,
                recent,
                LowStockProducts: []);

            return Result<AdminDashboardDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error building admin dashboard stats");
            return Result<AdminDashboardDto>.Failure(new Error("Dashboard.Unknown", "Error building dashboard stats."));
        }
    }
}
