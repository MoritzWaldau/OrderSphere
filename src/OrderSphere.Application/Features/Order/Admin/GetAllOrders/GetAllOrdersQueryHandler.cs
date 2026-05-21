using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Order.Admin.GetAllOrders;

public sealed class GetAllOrdersQueryHandler(IOrderingClient orderingClient)
    : IQueryHandler<GetAllOrdersQuery, Result<IReadOnlyList<OrderDto>>>
{
    public Task<Result<IReadOnlyList<OrderDto>>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
        => orderingClient.GetAllOrdersAsync(request.StatusFilter, cancellationToken);
}
