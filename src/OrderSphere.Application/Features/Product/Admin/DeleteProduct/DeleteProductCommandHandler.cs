using OrderSphere.Application.Abstraction;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Product.Admin.DeleteProduct;

public sealed class DeleteProductCommandHandler(ICatalogClient catalogClient)
    : ICommandHandler<DeleteProductCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(DeleteProductCommand request, CancellationToken ct)
        => catalogClient.DeleteProductAsync(request.ProductId, ct);
}
