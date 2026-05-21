using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Order.GetOrderById;

public sealed class GetOrderByIdQueryHandler(IOrderingClient orderingClient)
    : IQueryHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    public Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        => orderingClient.GetOrderByIdAsync(request.OrderId, request.CustomerId, cancellationToken);
}
