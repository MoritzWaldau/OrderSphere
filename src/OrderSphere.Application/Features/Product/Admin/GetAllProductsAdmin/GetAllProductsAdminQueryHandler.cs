using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models.Admin;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Product.Admin.GetAllProductsAdmin;

public sealed class GetAllProductsAdminQueryHandler(ICatalogClient catalogClient)
    : IQueryHandler<GetAllProductsAdminQuery, Result<IReadOnlyList<AdminProductDto>>>
{
    public async Task<Result<IReadOnlyList<AdminProductDto>>> Handle(GetAllProductsAdminQuery request, CancellationToken ct)
    {
        var result = await catalogClient.GetAllProductsAdminAsync(ct);
        return result.IsSuccess
            ? Result<IReadOnlyList<AdminProductDto>>.Success(result.Value.ToList())
            : Result<IReadOnlyList<AdminProductDto>>.Failure(result.Error);
    }
}
