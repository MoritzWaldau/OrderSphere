using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Application.Models;
using OrderSphere.Ordering.Domain.Errors;

namespace OrderSphere.Ordering.Application.Features.Order;

public sealed record GetOrdersByCustomerQuery(Guid CustomerId)
    : IQuery<Result<IReadOnlyList<OrderDto>>>;

public sealed class GetOrdersByCustomerQueryHandler(
    IOrderingDbContext context,
    ILogger<GetOrdersByCustomerQueryHandler> logger
) : IQueryHandler<GetOrdersByCustomerQuery, Result<IReadOnlyList<OrderDto>>>
{
    public async Task<Result<IReadOnlyList<OrderDto>>> Handle(
        GetOrdersByCustomerQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var orders = await context.Orders
                .AsNoTracking()
                .Where(o => o.CustomerId == CustomerId.From(request.CustomerId))
                .OrderByDescending(o => o.CreatedAt)
                .Include(o => o.Items)
                .ToListAsync(cancellationToken);

            if (orders.Count == 0)
                return Result<IReadOnlyList<OrderDto>>.Success(Array.Empty<OrderDto>());

            var dtos = orders.Select(o => ToDto(o)).ToList();
            return Result<IReadOnlyList<OrderDto>>.Success(dtos);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving orders for customer {CustomerId}", request.CustomerId);
            return Result<IReadOnlyList<OrderDto>>.Failure(OrderErrors.UnknownError);
        }
    }

    internal static OrderDto ToDto(Domain.Entities.Order o)
    {
        var subtotal = o.Items.Sum(i => i.Price * i.Quantity);
        return new(
            o.Id.Value, o.CustomerId.Value, o.Status, o.PaymentMethod, o.TrackingNumber,
            new OrderShippingAddressDto(
                o.ShippingAddress.FirstName, o.ShippingAddress.LastName,
                o.ShippingAddress.Street, o.ShippingAddress.City,
                o.ShippingAddress.PostalCode, o.ShippingAddress.Country),
            o.Items.Select(i => new OrderLineDto(i.ProductId.Value, i.ProductName, i.Quantity, i.Price)).ToList(),
            subtotal - o.DiscountAmount,
            o.DiscountAmount,
            o.CouponCode,
            o.CreatedAt);
    }
}
