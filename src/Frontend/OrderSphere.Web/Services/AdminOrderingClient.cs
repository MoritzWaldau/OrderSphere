using System.Net.Http.Json;
using OrderSphere.Web.Models;

namespace OrderSphere.Web.Services;

public interface IAdminOrderingClient
{
    Task<ApiResult<List<OrderDto>>> GetOrdersAsync(string? statusFilter = null, CancellationToken ct = default);
    Task<ApiResult<OrderStatsDto>> GetStatsAsync(CancellationToken ct = default);
    Task<ApiResult<OrderDto>> GetOrderByIdAsync(Guid id, CancellationToken ct = default);
    Task<ApiResult> UpdateStatusAsync(Guid id, int newStatus, CancellationToken ct = default);
    Task<ApiResult> CancelOrderAsync(Guid id, CancellationToken ct = default);
}

public sealed class AdminOrderingClient(HttpClient client) : IAdminOrderingClient
{
    public Task<ApiResult<List<OrderDto>>> GetOrdersAsync(string? statusFilter = null, CancellationToken ct = default)
    {
        var url = "/api/v1/admin/orders";
        if (!string.IsNullOrEmpty(statusFilter))
        {
            // Map display name to enum int (Created=0, Paid=1, Shipped=2, Delivered=3, Cancelled=4)
            var statusInt = statusFilter switch
            {
                "Created" => 0,
                "Paid" => 1,
                "Shipped" => 2,
                "Delivered" => 3,
                "Cancelled" => 4,
                _ => -1
            };
            if (statusInt >= 0) url += $"?status={statusInt}";
        }
        return client.GetApiAsync<List<OrderDto>>(url, ct);
    }

    public Task<ApiResult<OrderStatsDto>> GetStatsAsync(CancellationToken ct = default)
        => client.GetApiAsync<OrderStatsDto>("/api/v1/admin/orders/stats", ct);

    public Task<ApiResult<OrderDto>> GetOrderByIdAsync(Guid id, CancellationToken ct = default)
        => client.GetApiAsync<OrderDto>($"/api/v1/admin/orders/{id}", ct);

    public Task<ApiResult> UpdateStatusAsync(Guid id, int newStatus, CancellationToken ct = default)
        => client.SendApiAsync(
            new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/orders/{id}/status")
            {
                Content = JsonContent.Create(new { NewStatus = newStatus })
            }, ct);

    public Task<ApiResult> CancelOrderAsync(Guid id, CancellationToken ct = default)
        => client.SendApiAsync(new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/orders/{id}/cancel"), ct);
}
