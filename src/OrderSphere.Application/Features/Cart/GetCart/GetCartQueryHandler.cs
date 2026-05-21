using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Cart.GetCart;

public sealed class GetCartQueryHandler(IOrderingClient orderingClient)
    : IQueryHandler<GetCartQuery, Result<CartDto>>
{
    public Task<Result<CartDto>> Handle(GetCartQuery request, CancellationToken cancellationToken)
        => orderingClient.GetCartAsync(request.CustomerId, cancellationToken);
}
