using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Order.GetOrderByCorrelationId;

public sealed class GetOrderByCorrelationIdQueryHandler(IOrderingClient orderingClient)
    : IQueryHandler<GetOrderByCorrelationIdQuery, Result<OrderDto?>>
{
    public Task<Result<OrderDto?>> Handle(GetOrderByCorrelationIdQuery request, CancellationToken cancellationToken)
        => orderingClient.GetOrderByCorrelationIdAsync(request.CorrelationId, request.CustomerId, cancellationToken);
}
