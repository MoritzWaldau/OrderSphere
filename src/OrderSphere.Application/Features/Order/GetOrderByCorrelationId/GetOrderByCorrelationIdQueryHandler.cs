using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Order.GetOrderByCorrelationId;

public sealed class GetOrderByCorrelationIdQueryHandler(
    IDbContext context,
    ILogger<GetOrderByCorrelationIdQueryHandler> logger
) : IQueryHandler<GetOrderByCorrelationIdQuery, Result<OrderDto?>>
{
    public async Task<Result<OrderDto?>> Handle(GetOrderByCorrelationIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(
                    o => o.CorrelationId == request.CorrelationId && !o.IsDeleted,
                    cancellationToken);

            if (order is null)
            {
                // Not yet processed by the worker — return null in a successful result
                // so the caller can poll without an error path.
                return Result<OrderDto?>.Success(null);
            }

            if (order.CustomerId != request.CustomerId)
            {
                logger.LogWarning(
                    "Customer {CustomerId} attempted to access order with foreign correlationId {CorrelationId}",
                    request.CustomerId, request.CorrelationId);
                return Result<OrderDto?>.Success(null);
            }

            var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
            var productNames = await context.Products
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Name })
                .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

            var dto = new OrderDto(
                order.Id,
                order.CustomerId,
                order.Status,
                order.PaymentMethod,
                order.TrackingNumber,
                new OrderShippingAddressDto(
                    order.ShippingAddress.FirstName,
                    order.ShippingAddress.LastName,
                    order.ShippingAddress.Street,
                    order.ShippingAddress.City,
                    order.ShippingAddress.PostalCode,
                    order.ShippingAddress.Country),
                order.Items.Select(i => new OrderLineDto(
                    i.ProductId,
                    productNames.TryGetValue(i.ProductId, out var name) ? name : "Unbekanntes Produkt",
                    i.Quantity,
                    i.Price)).ToList(),
                order.Items.Sum(i => i.Price * i.Quantity),
                order.CreatedAt
            );

            return Result<OrderDto?>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error retrieving order by correlationId {CorrelationId}",
                request.CorrelationId);
            return Result<OrderDto?>.Failure(OrderErrors.UnknownError);
        }
    }
}
