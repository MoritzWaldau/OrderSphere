using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Application.Models;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Domain.Errors;

namespace OrderSphere.Ordering.Application.Features.Order.Admin;

public sealed record GetOrderStatsQuery : IQuery<Result<OrderStatsDto>>;

public sealed class GetOrderStatsQueryHandler(
    IOrderingDbContext context,
    ILogger<GetOrderStatsQueryHandler> logger
) : IQueryHandler<GetOrderStatsQuery, Result<OrderStatsDto>>
{
    public async Task<Result<OrderStatsDto>> Handle(GetOrderStatsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var todayUtc = DateTime.UtcNow.Date;

            var totalOrders = await context.Orders
                .AsNoTracking()
                .CountAsync(cancellationToken);

            var ordersToday = await context.Orders
                .AsNoTracking()
                .CountAsync(o => o.CreatedAt >= todayUtc, cancellationToken);

            var pendingShipments = await context.Orders
                .AsNoTracking()
                .CountAsync(o => o.Status == OrderStatus.Paid, cancellationToken);

            var totalCustomers = await context.Orders
                .AsNoTracking()
                .Select(o => o.CustomerId)
                .Distinct()
                .CountAsync(cancellationToken);

            // Price is a ComplexProperty → i.Price.Amount maps to the "price" column.
            // Quantity uses ValueConverter<Quantity,int> → (int)i.Quantity maps to the
            // "quantity" column; the Convert node is a no-op over the already-int column.
            var totalRevenue = await context.Orders
                .AsNoTracking()
                .Where(o => o.Status != OrderStatus.Cancelled)
                .SelectMany(o => o.Items)
                .SumAsync(i => i.Price.Amount * (int)i.Quantity, cancellationToken);

            var revenueToday = await context.Orders
                .AsNoTracking()
                .Where(o => o.Status != OrderStatus.Cancelled && o.CreatedAt >= todayUtc)
                .SelectMany(o => o.Items)
                .SumAsync(i => i.Price.Amount * (int)i.Quantity, cancellationToken);

            // o.Items.Sum(...) inside Select translates to a correlated subquery.
            // o.Status.ToString() and o.Id.Value are not SQL-translatable; they are
            // resolved after materialisation.
            var recentRaw = await context.Orders
                .AsNoTracking()
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new
                {
                    o.Id,
                    FirstName = o.ShippingAddress.FirstName,
                    LastName = o.ShippingAddress.LastName,
                    Total = o.Items.Sum(i => i.Price.Amount * (int)i.Quantity),
                    o.Status,
                    o.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            var recent = recentRaw
                .Select(o => new RecentOrderDto(
                    o.Id.Value,
                    $"{o.FirstName} {o.LastName}",
                    o.Total,
                    o.Status.ToString(),
                    o.CreatedAt))
                .ToList();

            return Result<OrderStatsDto>.Success(new OrderStatsDto(
                totalOrders, ordersToday, totalRevenue, revenueToday,
                pendingShipments, totalCustomers, recent));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error building order stats");
            return Result<OrderStatsDto>.Failure(OrderErrors.UnknownError);
        }
    }
}
