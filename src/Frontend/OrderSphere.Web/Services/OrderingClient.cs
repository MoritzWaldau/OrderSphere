using System.Net;
using System.Net.Http.Json;
using OrderSphere.Web.Models;

namespace OrderSphere.Web.Services;

public interface IOrderingClient
{
    Task<ApiResult<CartDto>> GetCartAsync(CancellationToken ct = default);
    Task<ApiResult> AddToCartAsync(Guid productId, int quantity, CancellationToken ct = default);
    Task<ApiResult> RemoveFromCartAsync(Guid productId, CancellationToken ct = default);
    Task<ApiResult> DecreaseCartItemAsync(Guid productId, CancellationToken ct = default);
    Task<ApiResult<List<OrderDto>>> GetOrdersByCustomerAsync(CancellationToken ct = default);
    Task<ApiResult<OrderDto>> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default);
    Task<OrderDto?> GetOrderByCorrelationIdAsync(Guid correlationId, CancellationToken ct = default);
    Task<ApiResult<Guid>> CheckoutAsync(CheckoutRequest request, Guid idempotencyKey, CancellationToken ct = default);
    Task<ApiResult<CouponValidationDto>> ValidateCouponAsync(string code, decimal subtotal, CancellationToken ct = default);
}

public sealed class OrderingClient(HttpClient client) : IOrderingClient
{
    public Task<ApiResult<CartDto>> GetCartAsync(CancellationToken ct = default)
        => client.GetApiAsync<CartDto>("/api/v1/cart", ct);

    public Task<ApiResult> AddToCartAsync(Guid productId, int quantity, CancellationToken ct = default)
        => client.SendApiAsync(
            new HttpRequestMessage(HttpMethod.Post, "/api/v1/cart/add")
            {
                Content = JsonContent.Create(new { ProductId = productId, Quantity = quantity })
            }, ct);

    public Task<ApiResult> RemoveFromCartAsync(Guid productId, CancellationToken ct = default)
        => client.SendApiAsync(
            new HttpRequestMessage(HttpMethod.Delete, "/api/v1/cart/remove")
            {
                Content = JsonContent.Create(new { ProductId = productId })
            }, ct);

    public Task<ApiResult> DecreaseCartItemAsync(Guid productId, CancellationToken ct = default)
        => client.SendApiAsync(
            new HttpRequestMessage(HttpMethod.Put, "/api/v1/cart/decrease")
            {
                Content = JsonContent.Create(new { ProductId = productId })
            }, ct);

    public Task<ApiResult<List<OrderDto>>> GetOrdersByCustomerAsync(CancellationToken ct = default)
        => client.GetApiAsync<List<OrderDto>>("/api/v1/orders", ct);

    public Task<ApiResult<OrderDto>> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default)
        => client.GetApiAsync<OrderDto>($"/api/v1/orders/{orderId}", ct);

    public async Task<OrderDto?> GetOrderByCorrelationIdAsync(Guid correlationId, CancellationToken ct = default)
    {
        var response = await client.GetAsync($"/api/v1/orders/correlation/{correlationId}", ct);
        // 204 = order not persisted yet (worker still processing); any non-success = not available.
        // Kept nullable on purpose: null means "keep polling", not "error".
        if (response.StatusCode == HttpStatusCode.NoContent || !response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadFromJsonAsync<OrderDto>(ct);
    }

    public async Task<ApiResult<Guid>> CheckoutAsync(CheckoutRequest request, Guid idempotencyKey, CancellationToken ct = default)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/checkout")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("Idempotency-Key", idempotencyKey.ToString());

        var result = await client.SendApiAsync<CheckoutResult>(httpRequest, ct);
        return result.IsSuccess
            ? ApiResult<Guid>.Ok(result.Value!.CorrelationId)
            : ApiResult<Guid>.Fail(result.Error!);
    }

    public Task<ApiResult<CouponValidationDto>> ValidateCouponAsync(string code, decimal subtotal, CancellationToken ct = default)
        => client.GetApiAsync<CouponValidationDto>(
            // Invariant formatting: the backend binds [FromQuery] decimal with InvariantCulture.
            $"/api/v1/coupons/validate?code={Uri.EscapeDataString(code)}&subtotal={subtotal.ToString(System.Globalization.CultureInfo.InvariantCulture)}", ct);

    private sealed record CheckoutResult(Guid CorrelationId);
}
