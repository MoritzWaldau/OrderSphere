namespace OrderSphere.Application.Models.Admin;

public sealed record AdminDashboardDto(
    int TotalOrders,
    int OrdersToday,
    decimal TotalRevenue,
    decimal RevenueToday,
    int PendingShipments,
    int LowStockProductsCount,
    int TotalProducts,
    int TotalUsers,
    IReadOnlyList<RecentOrderDto> RecentOrders,
    IReadOnlyList<LowStockProductDto> LowStockProducts);

public sealed record RecentOrderDto(
    Guid OrderId,
    string CustomerName,
    decimal Total,
    string Status,
    DateTime CreatedAt);

public sealed record LowStockProductDto(
    Guid ProductId,
    string Name,
    string SKU,
    int Stock);
