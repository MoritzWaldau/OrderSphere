using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Domain.Errors;
using OrderSphere.Ordering.Domain.Events;

namespace OrderSphere.Ordering.Application.Features.Checkout;

public sealed class CheckoutCartCommandHandler(
    ICatalogClient catalogClient,
    IBasketClient basketClient,
    IOrderingServiceBusPublisher serviceBusPublisher,
    ICheckoutIdempotencyStore idempotencyStore,
    ILogger<CheckoutCartCommandHandler> logger
) : ICommandHandler<CheckoutCartCommand, Result<Guid>>
{
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);

    public async Task<Result<Guid>> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
    {
        // Idempotency guard: return the stored result for a key we have already processed.
        // Distributed (Redis) so the guard holds across service instances.
        var cacheKey = $"checkout:{request.CustomerId}:{request.IdempotencyKey}";
        var cachedCorrelationId = await idempotencyStore.TryGetCorrelationIdAsync(cacheKey, cancellationToken);
        if (cachedCorrelationId is { } existingCorrelationId)
        {
            logger.LogInformation(
                "Duplicate checkout request for customer {CustomerId}. Returning cached CorrelationId: {CorrelationId}",
                request.CustomerId, existingCorrelationId);
            return Result<Guid>.Success(existingCorrelationId);
        }

        // Tracks items whose stock was decremented so they can be restored on failure.
        // ProductId kept typed; ICatalogClient calls convert to .Value at the boundary.
        var decremented = new List<(ProductId ProductId, int Quantity)>();

        try
        {
            // 1. Fetch cart. ICatalogClient still uses Guid — convert at service boundary.
            var cartResult = await basketClient.GetCartAsync(request.CustomerId.Value, cancellationToken);
            if (cartResult.IsFailure)
            {
                logger.LogError("Cart not found for customer {CustomerId} via Basket service", request.CustomerId);
                return Result<Guid>.Failure(CheckoutCartErrors.CartNotFoundError);
            }

            var cart = cartResult.Value;
            if (cart.Items.Count == 0)
            {
                logger.LogWarning("Checkout attempted on empty cart for customer {CustomerId}", request.CustomerId);
                return Result<Guid>.Failure(CheckoutCartErrors.EmptyCartError);
            }

            // 2. Resolve all product details — read-only, no state change yet.
            var orderItemDtos = new List<OrderItemEventDto>(cart.Items.Count);
            foreach (var cartItem in cart.Items)
            {
                var productResult = await catalogClient.GetProductByIdAsync(cartItem.ProductId, cancellationToken);
                if (productResult.IsFailure)
                {
                    logger.LogWarning("Product {ProductId} not found in Catalog during checkout for customer {CustomerId}",
                        cartItem.ProductId, request.CustomerId);
                    return Result<Guid>.Failure(ProductErrors.ProductNotFoundError);
                }

                orderItemDtos.Add(new OrderItemEventDto(
                    cartItem.ProductId,
                    productResult.Value.Name,
                    cartItem.Quantity,
                    productResult.Value.Price));
            }

            // 3. Decrement stock — every success is tracked for compensation.
            foreach (var item in orderItemDtos)
            {
                var decrementResult = await catalogClient.DecrementStockAsync(
                    item.ProductId, item.Quantity, cancellationToken);

                if (decrementResult.IsFailure)
                {
                    logger.LogWarning("Stock decrement failed for product {ProductId}; compensating {Count} previously decremented item(s)",
                        item.ProductId, decremented.Count);
                    return Result<Guid>.Failure(ProductErrors.InsufficientStockError);
                    // finally block restores decremented items
                }

                decremented.Add((ProductId.From(item.ProductId), item.Quantity));
            }

            // 4. Publish to Service Bus — if this throws, finally compensates.
            // CorrelationId is derived deterministically so retries produce the same value,
            // letting the Worker's CorrelationId uniqueness check deduplicate concurrent or
            // repeated checkout attempts for the same IdempotencyKey.
            var correlationId = DeriveCorrelationId(request.CustomerId, request.IdempotencyKey);
            var checkoutCartEvent = new CheckoutCartEvent(
                correlationId,
                new CheckoutCartDto(
                    request.CustomerId.Value,
                    request.CustomerEmail,
                    request.CustomerName,
                    request.ShippingAddress,
                    request.PaymentMethod,
                    request.CouponCode),
                orderItemDtos);

            await serviceBusPublisher.PublishCheckoutCartEventAsync(checkoutCartEvent, cancellationToken);

            await idempotencyStore.SetCorrelationIdAsync(cacheKey, correlationId, IdempotencyTtl, cancellationToken);
            decremented.Clear();

            // 5. Clear cart. Best-effort: the order is already committed to the bus.
            var clearResult = await basketClient.ClearCartItemsAsync(request.CustomerId.Value, cancellationToken);
            if (clearResult.IsFailure)
            {
                logger.LogWarning(
                    "Cart clear failed for customer {CustomerId} after accepted checkout. CorrelationId: {CorrelationId}. Cart may appear stale.",
                    request.CustomerId, correlationId);
            }

            logger.LogInformation(
                "Checkout accepted for customer {CustomerId}. CorrelationId: {CorrelationId}",
                request.CustomerId, correlationId);

            return Result<Guid>.Success(correlationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Checkout failed for customer {CustomerId}", request.CustomerId);
            return Result<Guid>.Failure(CheckoutCartErrors.UnknownError);
        }
        finally
        {
            if (decremented.Count > 0)
            {
                logger.LogWarning(
                    "Restoring stock for {Count} item(s) after checkout failure for customer {CustomerId}",
                    decremented.Count, request.CustomerId);

                foreach (var (productId, quantity) in decremented)
                {
                    var restoreResult = await catalogClient.RestoreStockAsync(
                        productId.Value, quantity, CancellationToken.None);

                    if (restoreResult.IsFailure)
                    {
                        logger.LogError(
                            "COMPENSATION FAILED: stock restore for product {ProductId} (qty {Quantity}) did not succeed. Manual reconciliation required.",
                            productId, quantity);
                    }
                }
            }
        }
    }

    private static Guid DeriveCorrelationId(CustomerId customerId, Guid idempotencyKey)
    {
        var input = Encoding.UTF8.GetBytes($"{customerId.Value}:{idempotencyKey}");
        var hash = SHA256.HashData(input);
        return new Guid(hash.AsSpan()[..16]);
    }
}
