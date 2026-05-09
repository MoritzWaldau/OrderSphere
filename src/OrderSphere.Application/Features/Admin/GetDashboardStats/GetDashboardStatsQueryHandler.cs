using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models.Admin;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Enums;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Admin.GetDashboardStats;

public sealed class GetDashboardStatsQueryHandler(
    IDbContext context,
    IUserAdminService userAdminService,
    ILogger<GetDashboardStatsQueryHandler> logger
) : IQueryHandler<GetDashboardStatsQuery, Result<AdminDashboardDto>>
{
    private const int LowStockThreshold = 10;

    public async Task<Result<AdminDashboardDto>> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var todayStart = DateTime.UtcNow.Date;

            var allOrders = await context.Orders
                .AsNoTracking()
                .Where(o => !o.IsDeleted)
                .Include(o => o.Items)
                .ToListAsync(cancellationToken);

            var nonCancelledOrders = allOrders.Where(o => o.Status != OrderStatus.Cancelled).ToList();

            var totalOrders = allOrders.Count;
            var ordersToday = allOrders.Count(o => o.CreatedAt >= todayStart);

            var totalRevenue = nonCancelledOrders.Sum(o => o.Items.Sum(i => i.Price * i.Quantity));
            var revenueToday = nonCancelledOrders
                .Where(o => o.CreatedAt >= todayStart)
                .Sum(o => o.Items.Sum(i => i.Price * i.Quantity));

            var pendingShipments = allOrders.Count(o => o.Status == OrderStatus.Paid);

            var lowStock = await context.Products
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.IsActive && p.Stock < LowStockThreshold)
                .OrderBy(p => p.Stock)
                .Take(10)
                .Select(p => new LowStockProductDto(p.Id, p.Name, p.SKU, p.Stock))
                .ToListAsync(cancellationToken);

            var totalProducts = await context.Products
                .CountAsync(p => !p.IsDeleted, cancellationToken);

            var users = await userAdminService.GetAllUsersAsync(cancellationToken);
            var totalUsers = users.Count;

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

            var dto = new AdminDashboardDto(
                totalOrders,
                ordersToday,
                totalRevenue,
                revenueToday,
                pendingShipments,
                lowStock.Count,
                totalProducts,
                totalUsers,
                recent,
                lowStock);

            return Result<AdminDashboardDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error building admin dashboard stats");
            return Result<AdminDashboardDto>.Failure(OrderErrors.UnknownError);
        }
    }
}
