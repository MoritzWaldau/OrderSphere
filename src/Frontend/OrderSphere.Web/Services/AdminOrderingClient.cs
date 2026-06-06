using System.Net.Http.Json;
using OrderSphere.Web.Models;

namespace OrderSphere.Web.Services;

public interface IAdminOrderingClient
{
    Task<List<OrderDto>> GetOrdersAsync(string? statusFilter = null, CancellationToken ct = default);
    Task<OrderStatsDto?> GetStatsAsync(CancellationToken ct = default);
    Task<OrderDto?> GetOrderByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> UpdateStatusAsync(Guid id, int newStatus, CancellationToken ct = default);
    Task<bool> CancelOrderAsync(Guid id, CancellationToken ct = default);
}

public sealed class AdminOrderingClient : IAdminOrderingClient
{
    private readonly HttpClient _client;
    public AdminOrderingClient(HttpClient client) => _client = client;

    public async Task<List<OrderDto>> GetOrdersAsync(string? statusFilter = null, CancellationToken ct = default)
    {
        var url = "/api/v1/admin/orders";
        if (!string.IsNullOrEmpty(statusFilter))
        {
            // Map display name to enum int (Created=0, Paid=1, Shipped=2, Delivered=3, Cancelled=4)
            var statusInt = statusFilter switch
            {
                "Created" => 0, "Paid" => 1, "Shipped" => 2, "Delivered" => 3, "Cancelled" => 4,
                _ => -1
            };
            if (statusInt >= 0) url += $"?status={statusInt}";
        }
        var result = await _client.GetFromJsonAsync<List<OrderDto>>(url, ct);
        return result ?? [];
    }

    public async Task<OrderStatsDto?> GetStatsAsync(CancellationToken ct = default)
    {
        var response = await _client.GetAsync("/api/v1/admin/orders/stats", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<OrderStatsDto>(ct);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _client.GetAsync($"/api/v1/admin/orders/{id}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<OrderDto>(ct);
    }

    public async Task<bool> UpdateStatusAsync(Guid id, int newStatus, CancellationToken ct = default)
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/v1/admin/orders/{id}/status",
            new { NewStatus = newStatus }, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CancelOrderAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _client.PostAsync($"/api/v1/admin/orders/{id}/cancel", null, ct);
        return response.IsSuccessStatusCode;
    }
}
