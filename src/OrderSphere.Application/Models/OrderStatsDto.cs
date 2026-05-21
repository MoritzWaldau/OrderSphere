namespace OrderSphere.Application.Models;

public sealed record OrderStatsDto(
    int TotalOrders,
    int OrdersToday,
    decimal TotalRevenue,
    decimal RevenueToday,
    int PendingShipments,
    int TotalCustomers,
    IReadOnlyList<RecentOrderSummaryDto> RecentOrders);

public sealed record RecentOrderSummaryDto(
    Guid Id,
    string CustomerName,
    decimal Total,
    string Status,
    DateTime CreatedAt);
