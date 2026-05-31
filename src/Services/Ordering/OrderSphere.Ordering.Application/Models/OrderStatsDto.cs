namespace OrderSphere.Ordering.Application.Models;

public sealed record OrderStatsDto(
    int TotalOrders,
    int OrdersToday,
    decimal TotalRevenue,
    decimal RevenueToday,
    int PendingShipments,
    int TotalCustomers,
    IReadOnlyList<RecentOrderDto> RecentOrders);

public sealed record RecentOrderDto(
    Guid Id,
    string CustomerName,
    decimal Total,
    string Status,
    DateTime CreatedAt);
