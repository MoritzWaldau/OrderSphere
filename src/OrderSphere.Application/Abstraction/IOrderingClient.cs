using OrderSphere.Application.Models;
using OrderSphere.Application.Models.Admin;
using OrderSphere.Domain.Enums;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Abstraction;

public interface IOrderingClient
{
    // Cart
    Task<Result<CartDto>> GetCartAsync(Guid customerId, CancellationToken ct = default);
    Task<Result> AddToCartAsync(Guid customerId, Guid productId, int quantity, CancellationToken ct = default);
    Task<Result> RemoveFromCartAsync(Guid customerId, Guid productId, CancellationToken ct = default);
    Task<Result> DecreaseCartItemQuantityAsync(Guid customerId, Guid productId, CancellationToken ct = default);

    // Checkout
    Task<Result<Guid>> CheckoutAsync(CheckoutCartDto dto, CancellationToken ct = default);

    // Coupon
    Task<Result<CouponValidationDto>> ValidateCouponAsync(string code, decimal subtotal, CancellationToken ct = default);

    // Orders (customer)
    Task<Result<IReadOnlyList<OrderDto>>> GetOrdersByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<Result<OrderDto>> GetOrderByIdAsync(Guid orderId, Guid customerId, CancellationToken ct = default);
    Task<Result<OrderDto?>> GetOrderByCorrelationIdAsync(Guid correlationId, Guid customerId, CancellationToken ct = default);

    // Orders (admin)
    Task<Result<IReadOnlyList<OrderDto>>> GetAllOrdersAsync(OrderStatus? statusFilter, CancellationToken ct = default);
    Task<Result<OrderDto>> GetOrderByIdAdminAsync(Guid orderId, CancellationToken ct = default);
    Task<Result<bool>> UpdateOrderStatusAsync(Guid orderId, OrderStatus newStatus, CancellationToken ct = default);
    Task<Result<bool>> CancelOrderAsync(Guid orderId, CancellationToken ct = default);

    // Stats
    Task<Result<OrderStatsDto>> GetOrderStatsAsync(CancellationToken ct = default);
}
