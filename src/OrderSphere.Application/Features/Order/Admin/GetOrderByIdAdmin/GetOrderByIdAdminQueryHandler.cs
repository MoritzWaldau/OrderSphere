using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Order.Admin.GetOrderByIdAdmin;

public sealed class GetOrderByIdAdminQueryHandler(IOrderingClient orderingClient)
    : IQueryHandler<GetOrderByIdAdminQuery, Result<OrderDto>>
{
    public Task<Result<OrderDto>> Handle(GetOrderByIdAdminQuery request, CancellationToken cancellationToken)
        => orderingClient.GetOrderByIdAdminAsync(request.OrderId, cancellationToken);
}
