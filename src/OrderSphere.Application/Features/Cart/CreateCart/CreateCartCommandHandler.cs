using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Cart.CreateCart;

public sealed class CreateCartCommandHandler : ICommandHandler<CreateCartCommand, Result>
{
    public async Task<Result> Handle(CreateCartCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
