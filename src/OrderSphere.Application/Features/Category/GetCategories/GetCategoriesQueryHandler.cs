using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Category.GetCategories;

public sealed class GetCategoriesQueryHandler(ICatalogClient catalogClient)
    : IQueryHandler<GetCategoriesQuery, Result<IEnumerable<CategoryDto>>>
{
    public Task<Result<IEnumerable<CategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken ct)
        => catalogClient.GetCategoriesAsync(ct);
}
