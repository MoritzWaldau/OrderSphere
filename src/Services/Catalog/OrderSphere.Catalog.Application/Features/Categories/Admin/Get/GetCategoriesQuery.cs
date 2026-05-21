namespace OrderSphere.Catalog.Application.Features.Categories.Admin.Get;

public sealed record GetCategoriesQuery(int Page = 1, int PageSize = 50)
    : IRequest<Result<PagedResult<CategoryDto>>>;
