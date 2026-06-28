using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Domain.Errors;
using Entities = OrderSphere.Ordering.Domain.Entities;

namespace OrderSphere.Ordering.Application.Features.Returns.RequestReturn;

/// <summary>
/// Creates a customer-initiated return request. Validates ownership, that the order has progressed
/// far enough to be returnable, and that every requested line maps to an order line with a
/// sufficient quantity. The unit price is copied from the order so the eventual refund amount is
/// fixed at request time.
/// </summary>
public sealed class RequestReturnCommandHandler(IOrderingDbContext context)
    : ICommandHandler<RequestReturnCommand, Result<Guid>>
{
    private static readonly OrderStatus[] ReturnableStatuses =
        [OrderStatus.Paid, OrderStatus.Shipped, OrderStatus.Delivered];

    public async Task<Result<Guid>> Handle(RequestReturnCommand request, CancellationToken ct)
    {
        var order = await context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == OrderId.From(request.OrderId), ct);

        if (order is null)
            return Result<Guid>.Failure(ReturnErrors.OrderNotFound);

        if (order.CustomerId != CustomerId.From(request.CustomerId))
            return Result<Guid>.Failure(ReturnErrors.NotOrderOwner);

        if (!ReturnableStatuses.Contains(order.Status))
            return Result<Guid>.Failure(ReturnErrors.OrderNotReturnable);

        var returnItems = new List<Entities.ReturnItem>();
        foreach (var line in request.Items)
        {
            var orderLine = order.Items.FirstOrDefault(i => i.ProductId == ProductId.From(line.ProductId));
            if (orderLine is null)
                return Result<Guid>.Failure(ReturnErrors.UnknownItem);

            if (line.Quantity > orderLine.Quantity)
                return Result<Guid>.Failure(ReturnErrors.QuantityExceedsOrdered);

            returnItems.Add(new Entities.ReturnItem(
                orderLine.ProductId, orderLine.ProductName, line.Quantity, orderLine.Price));
        }

        // All order lines share one currency; take it from any line for the refund amount.
        var currency = order.Items.First().Price.Currency;

        var returnRequest = new Entities.ReturnRequest(
            OrderId.From(request.OrderId),
            CustomerId.From(request.CustomerId),
            request.Reason,
            currency,
            returnItems,
            DateTime.UtcNow);

        context.ReturnRequests.Add(returnRequest);
        await context.SaveChangesAsync(ct);

        return Result<Guid>.Success(returnRequest.Id.Value);
    }
}
