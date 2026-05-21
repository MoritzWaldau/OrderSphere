using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models.Admin;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Category.Admin.GetAllCategoriesAdmin;

public sealed class GetAllCategoriesAdminQueryHandler(ICatalogClient catalogClient)
    : IQueryHandler<GetAllCategoriesAdminQuery, Result<IReadOnlyList<AdminCategoryDto>>>
{
    public async Task<Result<IReadOnlyList<AdminCategoryDto>>> Handle(GetAllCategoriesAdminQuery request, CancellationToken ct)
    {
        var result = await catalogClient.GetAllCategoriesAdminAsync(ct);
        return result.IsSuccess
            ? Result<IReadOnlyList<AdminCategoryDto>>.Success(result.Value.ToList())
            : Result<IReadOnlyList<AdminCategoryDto>>.Failure(result.Error);
    }
}
