using OrderSphere.Ordering.Domain.Entities;

namespace OrderSphere.Ordering.Application.Models;

internal static class ReturnRequestMappings
{
    public static ReturnRequestDto ToDto(this ReturnRequest r) => new(
        r.Id.Value,
        r.OrderId.Value,
        r.CustomerId.Value,
        r.Status.ToString(),
        r.Reason,
        r.Resolution,
        r.RefundAmount,
        r.RequestedAt,
        r.ResolvedAt,
        r.Items.Select(i => new ReturnLineDto(i.ProductId.Value, i.ProductName, i.Quantity, i.UnitPrice)).ToList());
}
