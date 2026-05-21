using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Category.GetCategories;

public sealed class GetCategoriesQueryHandler(ICatalogClient catalogClient)
    : IQueryHandler<GetCategoriesQuery, Result<IEnumerable<CategoryDto>>>
{
    public Task<Result<IEnumerable<CategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken ct)
        => catalogClient.GetCategoriesAsync(ct);
}
