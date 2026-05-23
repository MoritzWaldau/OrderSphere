using System.Net.Http.Json;
using OrderSphere.Web.Models;

namespace OrderSphere.Web.Services;

public interface IOrderingClient
{
    Task<CartDto?> GetCartAsync(CancellationToken ct = default);
    Task<bool> AddToCartAsync(Guid productId, int quantity, CancellationToken ct = default);
    Task<bool> RemoveFromCartAsync(Guid productId, CancellationToken ct = default);
    Task<bool> DecreaseCartItemAsync(Guid productId, CancellationToken ct = default);
    Task<List<OrderDto>> GetOrdersByCustomerAsync(CancellationToken ct = default);
    Task<OrderDto?> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default);
    Task<OrderDto?> GetOrderByCorrelationIdAsync(Guid correlationId, CancellationToken ct = default);
    Task<Guid?> CheckoutAsync(CheckoutRequest request, CancellationToken ct = default);
    Task<CouponValidationDto?> ValidateCouponAsync(string code, decimal subtotal, CancellationToken ct = default);
}

public sealed class OrderingClient : IOrderingClient
{
    private readonly HttpClient _client;

    public OrderingClient(HttpClient client) => _client = client;

    public async Task<CartDto?> GetCartAsync(CancellationToken ct = default)
    {
        var response = await _client.GetAsync("/api/v1/cart", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<CartDto>(ct);
    }

    public async Task<bool> AddToCartAsync(Guid productId, int quantity, CancellationToken ct = default)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/cart/add",
            new { ProductId = productId, Quantity = quantity }, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveFromCartAsync(Guid productId, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/cart/remove")
        {
            Content = JsonContent.Create(new { ProductId = productId })
        };
        var response = await _client.SendAsync(request, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DecreaseCartItemAsync(Guid productId, CancellationToken ct = default)
    {
        var response = await _client.PutAsJsonAsync("/api/v1/cart/decrease",
            new { ProductId = productId }, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<OrderDto>> GetOrdersByCustomerAsync(CancellationToken ct = default)
    {
        var result = await _client.GetFromJsonAsync<List<OrderDto>>("/api/v1/orders", ct);
        return result ?? [];
    }

    public async Task<OrderDto?> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default)
    {
        var response = await _client.GetAsync($"/api/v1/orders/{orderId}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<OrderDto>(ct);
    }

    public async Task<OrderDto?> GetOrderByCorrelationIdAsync(Guid correlationId, CancellationToken ct = default)
    {
        var response = await _client.GetAsync($"/api/v1/orders/correlation/{correlationId}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<OrderDto>(ct);
    }

    public async Task<Guid?> CheckoutAsync(CheckoutRequest request, CancellationToken ct = default)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/checkout", request, ct);
        if (!response.IsSuccessStatusCode) return null;
        var result = await response.Content.ReadFromJsonAsync<CheckoutResult>(ct);
        return result?.CorrelationId;
    }

    public async Task<CouponValidationDto?> ValidateCouponAsync(string code, decimal subtotal, CancellationToken ct = default)
    {
        var response = await _client.GetAsync(
            $"/api/v1/coupons/validate?code={Uri.EscapeDataString(code)}&subtotal={subtotal}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<CouponValidationDto>(ct);
    }

    private sealed record CheckoutResult(Guid CorrelationId);
}
