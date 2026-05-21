using OrderSphere.Application.Abstraction;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Product.Admin.UpdateProduct;

public sealed class UpdateProductCommandHandler(ICatalogClient catalogClient)
    : ICommandHandler<UpdateProductCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(UpdateProductCommand request, CancellationToken ct)
        => catalogClient.UpdateProductAsync(request.ProductId, request.Input, ct);
}
