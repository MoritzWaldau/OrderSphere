using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Product.GetProduct;

public sealed class GetProductQueryHandler(ICatalogClient catalogClient)
    : IQueryHandler<GetProductQuery, Result<IEnumerable<ProductDto>>>
{
    public Task<Result<IEnumerable<ProductDto>>> Handle(GetProductQuery request, CancellationToken ct)
        => catalogClient.GetProductsAsync(ct);
}
