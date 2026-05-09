using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Order.GetOrderById;

public sealed class GetOrderByIdQueryHandler(
    IDbContext context,
    ILogger<GetOrderByIdQueryHandler> logger
) : IQueryHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(
                    o => o.Id == request.OrderId && !o.IsDeleted,
                    cancellationToken);

            if (order is null)
            {
                return Result<OrderDto>.Failure(OrderErrors.OrderNotFoundError);
            }

            // Authorization: only the owner may view their order
            if (order.CustomerId != request.CustomerId)
            {
                logger.LogWarning(
                    "Customer {CustomerId} attempted to access foreign order {OrderId}",
                    request.CustomerId,
                    request.OrderId);
                return Result<OrderDto>.Failure(OrderErrors.OrderNotFoundError);
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

            return Result<OrderDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error retrieving order {OrderId} for customer {CustomerId}",
                request.OrderId, request.CustomerId);
            return Result<OrderDto>.Failure(OrderErrors.UnknownError);
        }
    }
}
