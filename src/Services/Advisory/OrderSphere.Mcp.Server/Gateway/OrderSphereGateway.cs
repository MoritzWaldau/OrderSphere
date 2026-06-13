using System.Globalization;
using System.Net;
using System.Net.Http.Json;

namespace OrderSphere.Mcp.Server.Gateway;

// Typed wrapper over the API Gateway's public /api/v1 surface. Mirrors the
// existing typed-HTTP-client pattern (e.g. Ordering.Infrastructure/HttpCatalogClient).
// User-scoped calls rely on BearerForwardingHandler to attach the caller's token.
public interface IOrderSphereGateway
{
    Task<PagedResult<ProductDto>> GetProductsAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        string? categoryName = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        CancellationToken ct = default);
    Task<ProductDto?> GetProductBySlugAsync(string slug, CancellationToken ct = default);
    Task<PagedResult<CategoryDto>> GetCategoriesAsync(int page, int pageSize, CancellationToken ct = default);

    Task<IReadOnlyList<OrderDto>> GetMyOrdersAsync(CancellationToken ct = default);
    Task<OrderDto?> GetOrderAsync(Guid orderId, CancellationToken ct = default);
    Task<CouponValidationDto?> ValidateCouponAsync(string code, decimal subtotal, CancellationToken ct = default);

    Task<ProfileDto?> GetMyProfileAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AddressDto>> GetMyAddressesAsync(CancellationToken ct = default);

    Task<CartDto?> GetMyCartAsync(CancellationToken ct = default);

    Task<PaymentDto?> GetPaymentByOrderAsync(Guid orderId, CancellationToken ct = default);
}

public sealed class OrderSphereGateway(HttpClient http) : IOrderSphereGateway
{
    public async Task<PagedResult<ProductDto>> GetProductsAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        string? categoryName = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        CancellationToken ct = default)
    {
        var query = $"/api/v1/products?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
        if (!string.IsNullOrWhiteSpace(categoryName))
            query += $"&categoryName={Uri.EscapeDataString(categoryName)}";
        if (minPrice is { } min)
            query += $"&minPrice={min.ToString(CultureInfo.InvariantCulture)}";
        if (maxPrice is { } max)
            query += $"&maxPrice={max.ToString(CultureInfo.InvariantCulture)}";

        return await http.GetFromJsonAsync<PagedResult<ProductDto>>(query, ct)
            ?? new PagedResult<ProductDto>([], 0, page, pageSize);
    }

    public Task<ProductDto?> GetProductBySlugAsync(string slug, CancellationToken ct = default)
        => http.GetFromJsonAsync<ProductDto?>($"/api/v1/products/{Uri.EscapeDataString(slug)}", ct);

    public async Task<PagedResult<CategoryDto>> GetCategoriesAsync(int page, int pageSize, CancellationToken ct = default)
        => await http.GetFromJsonAsync<PagedResult<CategoryDto>>(
               $"/api/v1/categories?page={page}&pageSize={pageSize}", ct)
           ?? new PagedResult<CategoryDto>([], 0, page, pageSize);

    public async Task<IReadOnlyList<OrderDto>> GetMyOrdersAsync(CancellationToken ct = default)
        => await http.GetFromJsonAsync<List<OrderDto>>("/api/v1/orders", ct) ?? [];

    public async Task<OrderDto?> GetOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"/api/v1/orders/{orderId}", ct);
        return response.StatusCode == HttpStatusCode.OK
            ? await response.Content.ReadFromJsonAsync<OrderDto>(ct)
            : null;
    }

    public Task<CouponValidationDto?> ValidateCouponAsync(string code, decimal subtotal, CancellationToken ct = default)
        => http.GetFromJsonAsync<CouponValidationDto?>(
               $"/api/v1/coupons/validate?code={Uri.EscapeDataString(code)}&subtotal={subtotal}", ct);

    public async Task<ProfileDto?> GetMyProfileAsync(CancellationToken ct = default)
    {
        var response = await http.GetAsync("/api/v1/profile", ct);
        return response.StatusCode == HttpStatusCode.OK
            ? await response.Content.ReadFromJsonAsync<ProfileDto>(ct)
            : null;
    }

    public async Task<IReadOnlyList<AddressDto>> GetMyAddressesAsync(CancellationToken ct = default)
    {
        var response = await http.GetAsync("/api/v1/profile/addresses", ct);
        return response.StatusCode == HttpStatusCode.OK
            ? await response.Content.ReadFromJsonAsync<List<AddressDto>>(ct) ?? []
            : [];
    }

    public async Task<CartDto?> GetMyCartAsync(CancellationToken ct = default)
    {
        var response = await http.GetAsync("/api/v1/cart", ct);
        return response.StatusCode == HttpStatusCode.OK
            ? await response.Content.ReadFromJsonAsync<CartDto>(ct)
            : null;
    }

    public async Task<PaymentDto?> GetPaymentByOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"/api/v1/payments/by-order/{orderId}", ct);
        return response.StatusCode == HttpStatusCode.OK
            ? await response.Content.ReadFromJsonAsync<PaymentDto>(ct)
            : null;
    }
}
