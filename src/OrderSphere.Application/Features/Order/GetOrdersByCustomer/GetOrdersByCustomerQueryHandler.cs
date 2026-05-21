using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Order.GetOrdersByCustomer;

public sealed class GetOrdersByCustomerQueryHandler(IOrderingClient orderingClient)
    : IQueryHandler<GetOrdersByCustomerQuery, Result<IReadOnlyList<OrderDto>>>
{
    public Task<Result<IReadOnlyList<OrderDto>>> Handle(
        GetOrdersByCustomerQuery request, CancellationToken cancellationToken)
        => orderingClient.GetOrdersByCustomerAsync(request.CustomerId, cancellationToken);
}
