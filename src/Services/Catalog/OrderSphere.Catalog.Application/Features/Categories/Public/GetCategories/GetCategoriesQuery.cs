namespace OrderSphere.Catalog.Application.Features.Categories.Public.GetCategories;

public sealed record GetCategoriesQuery(int Page = 1, int PageSize = 50)
    : IRequest<Result<PagedResult<CategoryDto>>>;
