using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models;
using OrderSphere.Application.Models.Admin;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Domain.Enums;
using OrderSphere.Domain.Errors;
using System.Net.Http.Json;
using System.Text.Json;

namespace OrderSphere.Infrastructure.OrderingClient;

/// <summary>
/// HTTP proxy implementation of IOrderingClient.
/// Forwards all ordering operations to OrderSphere.Ordering.Api.
/// </summary>
public sealed class HttpOrderingClient(
    HttpClient httpClient,
    ILogger<HttpOrderingClient> logger) : IOrderingClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // ─── Cart ────────────────────────────────────────────────────────────────

    public async Task<Result<CartDto>> GetCartAsync(Guid customerId, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/v1/cart/{customerId}", ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return Result<CartDto>.Failure(CartErrors.CartNotFoundError);

            response.EnsureSuccessStatusCode();
            var dto = await response.Content.ReadFromJsonAsync<CartDto>(JsonOptions, ct);
            return dto is not null
                ? Result<CartDto>.Success(dto)
                : Result<CartDto>.Failure(CartErrors.CartNotFoundError);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Ordering.Api GET cart/{CustomerId}", customerId);
            return Result<CartDto>.Failure(CartErrors.UnknownError);
        }
    }

    public async Task<Result> AddToCartAsync(Guid customerId, Guid productId, int quantity, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/v1/cart/add",
                new { CustomerId = customerId, ProductId = productId, Quantity = quantity }, ct);

            return response.IsSuccessStatusCode
                ? Result.Success()
                : await MapErrorAsync(response, ct, CartErrors.UnknownError);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Ordering.Api POST cart/add");
            return Result.Failure(CartErrors.UnknownError);
        }
    }

    public async Task<Result> RemoveFromCartAsync(Guid customerId, Guid productId, CancellationToken ct = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/cart/remove")
            {
                Content = JsonContent.Create(new { CustomerId = customerId, ProductId = productId })
            };
            var response = await httpClient.SendAsync(request, ct);

            return response.IsSuccessStatusCode
                ? Result.Success()
                : await MapErrorAsync(response, ct, CartErrors.UnknownError);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Ordering.Api DELETE cart/remove");
            return Result.Failure(CartErrors.UnknownError);
        }
    }

    public async Task<Result> DecreaseCartItemQuantityAsync(Guid customerId, Guid productId, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync("/api/v1/cart/decrease",
                new { CustomerId = customerId, ProductId = productId }, ct);

            return response.IsSuccessStatusCode
                ? Result.Success()
                : await MapErrorAsync(response, ct, CartErrors.UnknownError);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Ordering.Api PUT cart/decrease");
            return Result.Failure(CartErrors.UnknownError);
        }
    }

    // ─── Checkout ────────────────────────────────────────────────────────────

    public async Task<Result<Guid>> CheckoutAsync(CheckoutCartDto dto, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/v1/checkout", dto, ct);
            if (!response.IsSuccessStatusCode)
                return Result<Guid>.Failure(await ReadErrorAsync(response, ct, CheckoutCartErrors.UnknownError));

            var body = await response.Content.ReadFromJsonAsync<CheckoutResponse>(JsonOptions, ct);
            return body is not null
                ? Result<Guid>.Success(body.CorrelationId)
                : Result<Guid>.Failure(CheckoutCartErrors.UnknownError);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Ordering.Api POST checkout");
            return Result<Guid>.Failure(CheckoutCartErrors.UnknownError);
        }
    }

    // ─── Coupon ──────────────────────────────────────────────────────────────

    public async Task<Result<CouponValidationDto>> ValidateCouponAsync(string code, decimal subtotal, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"/api/v1/coupon/validate?code={Uri.EscapeDataString(code)}&subtotal={subtotal}", ct);

            if (!response.IsSuccessStatusCode)
                return Result<CouponValidationDto>.Failure(await ReadErrorAsync(response, ct, CouponErrors.InvalidCode));

            var dto = await response.Content.ReadFromJsonAsync<CouponValidationDto>(JsonOptions, ct);
            return dto is not null
                ? Result<CouponValidationDto>.Success(dto)
                : Result<CouponValidationDto>.Failure(CouponErrors.InvalidCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Ordering.Api GET coupon/validate");
            return Result<CouponValidationDto>.Failure(CouponErrors.InvalidCode);
        }
    }

    // ─── Orders (customer) ───────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<OrderDto>>> GetOrdersByCustomerAsync(Guid customerId, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/v1/orders/customer/{customerId}", ct);
            response.EnsureSuccessStatusCode();
            var dtos = await response.Content.ReadFromJsonAsync<List<OrderDto>>(JsonOptions, ct);
            return Result<IReadOnlyList<OrderDto>>.Success(dtos ?? []);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Ordering.Api GET orders/customer/{CustomerId}", customerId);
            return Result<IReadOnlyList<OrderDto>>.Failure(OrderErrors.UnknownError);
        }
    }

    public async Task<Result<OrderDto>> GetOrderByIdAsync(Guid orderId, Guid customerId, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/v1/orders/{orderId}/customer/{customerId}", ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return Result<OrderDto>.Failure(OrderErrors.OrderNotFoundError);

            response.EnsureSuccessStatusCode();
            var dto = await response.Content.ReadFromJsonAsync<OrderDto>(JsonOptions, ct);
            return dto is not null
                ? Result<OrderDto>.Success(dto)
                : Result<OrderDto>.Failure(OrderErrors.OrderNotFoundError);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Ordering.Api GET orders/{OrderId}/customer/{CustomerId}", orderId, customerId);
            return Result<OrderDto>.Failure(OrderErrors.UnknownError);
        }
    }

    public async Task<Result<OrderDto?>> GetOrderByCorrelationIdAsync(Guid correlationId, Guid customerId, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"/api/v1/orders/correlation/{correlationId}/customer/{customerId}", ct);
            response.EnsureSuccessStatusCode();
            var dto = await response.Content.ReadFromJsonAsync<OrderDto?>(JsonOptions, ct);
            return Result<OrderDto?>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Ordering.Api GET orders/correlation/{CorrelationId}", correlationId);
            return Result<OrderDto?>.Failure(OrderErrors.UnknownError);
        }
    }

    // ─── Orders (admin) ──────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<OrderDto>>> GetAllOrdersAsync(OrderStatus? statusFilter, CancellationToken ct = default)
    {
        try
        {
            var url = statusFilter.HasValue
                ? $"/api/v1/admin/orders/?status={(int)statusFilter.Value}"
                : "/api/v1/admin/orders/";
            var response = await httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            var dtos = await response.Content.ReadFromJsonAsync<List<OrderDto>>(JsonOptions, ct);
            return Result<IReadOnlyList<OrderDto>>.Success(dtos ?? []);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Ordering.Api GET admin/orders");
            return Result<IReadOnlyList<OrderDto>>.Failure(OrderErrors.UnknownError);
        }
    }

    public async Task<Result<OrderDto>> GetOrderByIdAdminAsync(Guid orderId, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/v1/admin/orders/{orderId}", ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return Result<OrderDto>.Failure(OrderErrors.OrderNotFoundError);

            response.EnsureSuccessStatusCode();
            var dto = await response.Content.ReadFromJsonAsync<OrderDto>(JsonOptions, ct);
            return dto is not null
                ? Result<OrderDto>.Success(dto)
                : Result<OrderDto>.Failure(OrderErrors.OrderNotFoundError);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Ordering.Api GET admin/orders/{OrderId}", orderId);
            return Result<OrderDto>.Failure(OrderErrors.UnknownError);
        }
    }

    public async Task<Result<bool>> UpdateOrderStatusAsync(Guid orderId, OrderStatus newStatus, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync(
                $"/api/v1/admin/orders/{orderId}/status",
                new { NewStatus = (int)newStatus }, ct);

            return response.IsSuccessStatusCode
                ? Result<bool>.Success(true)
                : Result<bool>.Failure(await ReadErrorAsync(response, ct, OrderErrors.UnknownError));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Ordering.Api PUT admin/orders/{OrderId}/status", orderId);
            return Result<bool>.Failure(OrderErrors.UnknownError);
        }
    }

    public async Task<Result<bool>> CancelOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsync($"/api/v1/admin/orders/{orderId}/cancel", null, ct);
            return response.IsSuccessStatusCode
                ? Result<bool>.Success(true)
                : Result<bool>.Failure(await ReadErrorAsync(response, ct, OrderErrors.UnknownError));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Ordering.Api POST admin/orders/{OrderId}/cancel", orderId);
            return Result<bool>.Failure(OrderErrors.UnknownError);
        }
    }

    // ─── Stats ───────────────────────────────────────────────────────────────

    public async Task<Result<OrderStatsDto>> GetOrderStatsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync("/api/v1/admin/orders/stats", ct);
            response.EnsureSuccessStatusCode();
            var dto = await response.Content.ReadFromJsonAsync<OrderStatsDto>(JsonOptions, ct);
            return dto is not null
                ? Result<OrderStatsDto>.Success(dto)
                : Result<OrderStatsDto>.Failure(OrderErrors.UnknownError);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Ordering.Api GET admin/orders/stats");
            return Result<OrderStatsDto>.Failure(OrderErrors.UnknownError);
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static async Task<Result> MapErrorAsync(
        HttpResponseMessage response, CancellationToken ct, Error fallback)
        => Result.Failure(await ReadErrorAsync(response, ct, fallback));

    private static async Task<Error> ReadErrorAsync(
        HttpResponseMessage response, CancellationToken ct, Error fallback)
    {
        try
        {
            var err = await response.Content.ReadFromJsonAsync<ErrorPayload>(ct);
            return err is not null
                ? new Error(err.Code, err.Message)
                : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private sealed record CheckoutResponse(Guid CorrelationId);
    private sealed record ErrorPayload(string Code, string Message);
}
