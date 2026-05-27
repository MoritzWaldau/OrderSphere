using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Api.Models;
using OrderSphere.Ordering.Domain.Errors;
using OrderSphere.Ordering.Infrastructure.Persistence;

namespace OrderSphere.Ordering.Api.Features.Order;

public sealed record GetOrdersByCustomerQuery(Guid CustomerId)
    : IRequest<Result<IReadOnlyList<OrderDto>>>;

public sealed class GetOrdersByCustomerQueryHandler(
    IOrderingDbContext context,
    ILogger<GetOrdersByCustomerQueryHandler> logger
) : IRequestHandler<GetOrdersByCustomerQuery, Result<IReadOnlyList<OrderDto>>>
{
    public async Task<Result<IReadOnlyList<OrderDto>>> Handle(
        GetOrdersByCustomerQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var orders = await context.Orders
                .AsNoTracking()
                .Where(o => o.CustomerId == CustomerId.From(request.CustomerId) && !o.IsDeleted)
                .OrderByDescending(o => o.CreatedAt)
                .Include(o => o.Items)
                .ToListAsync(cancellationToken);

            if (orders.Count == 0)
                return Result<IReadOnlyList<OrderDto>>.Success(Array.Empty<OrderDto>());

            var dtos = orders.Select(o => ToDto(o)).ToList();
            return Result<IReadOnlyList<OrderDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving orders for customer {CustomerId}", request.CustomerId);
            return Result<IReadOnlyList<OrderDto>>.Failure(OrderErrors.UnknownError);
        }
    }

    internal static OrderDto ToDto(Domain.Entities.Order o) => new(
        o.Id.Value, o.CustomerId.Value, o.Status, o.PaymentMethod, o.TrackingNumber,
        new OrderShippingAddressDto(
            o.ShippingAddress.FirstName, o.ShippingAddress.LastName,
            o.ShippingAddress.Street, o.ShippingAddress.City,
            o.ShippingAddress.PostalCode, o.ShippingAddress.Country),
        o.Items.Select(i => new OrderLineDto(i.ProductId.Value, i.ProductName, i.Quantity, i.Price)).ToList(),
        o.Items.Sum(i => i.Price * i.Quantity),
        o.CreatedAt);
}
