using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Order.GetOrdersByCustomer;

public sealed class GetOrdersByCustomerQueryHandler(
    IDbContext context,
    ILogger<GetOrdersByCustomerQueryHandler> logger
) : IQueryHandler<GetOrdersByCustomerQuery, Result<IReadOnlyList<OrderDto>>>
{
    public async Task<Result<IReadOnlyList<OrderDto>>> Handle(
        GetOrdersByCustomerQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var orders = await context.Orders
                .AsNoTracking()
                .Where(o => o.CustomerId == request.CustomerId && !o.IsDeleted)
                .OrderByDescending(o => o.CreatedAt)
                .Include(o => o.Items)
                .ToListAsync(cancellationToken);

            if (orders.Count == 0)
            {
                return Result<IReadOnlyList<OrderDto>>.Success(Array.Empty<OrderDto>());
            }

            var productIds = orders
                .SelectMany(o => o.Items.Select(i => i.ProductId))
                .Distinct()
                .ToList();

            var productNames = await context.Products
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Name })
                .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

            var dtos = orders.Select(o => new OrderDto(
                o.Id,
                o.CustomerId,
                o.Status,
                o.PaymentMethod,
                new OrderShippingAddressDto(
                    o.ShippingAddress.FirstName,
                    o.ShippingAddress.LastName,
                    o.ShippingAddress.Street,
                    o.ShippingAddress.City,
                    o.ShippingAddress.PostalCode,
                    o.ShippingAddress.Country),
                o.Items.Select(i => new OrderLineDto(
                    i.ProductId,
                    productNames.TryGetValue(i.ProductId, out var name) ? name : "Unbekanntes Produkt",
                    i.Quantity,
                    i.Price)).ToList(),
                o.Items.Sum(i => i.Price * i.Quantity),
                o.CreatedAt
            )).ToList();

            return Result<IReadOnlyList<OrderDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving orders for customer {CustomerId}", request.CustomerId);
            return Result<IReadOnlyList<OrderDto>>.Failure(OrderErrors.UnknownError);
        }
    }
}
