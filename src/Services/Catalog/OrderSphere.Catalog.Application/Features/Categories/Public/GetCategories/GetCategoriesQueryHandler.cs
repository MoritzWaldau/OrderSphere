namespace OrderSphere.Catalog.Application.Features.Categories.Public.GetCategories;

public sealed class GetCategoriesQueryHandler(ICatalogDbContext context)
    : IRequestHandler<GetCategoriesQuery, Result<PagedResult<CategoryDto>>>
{
    public async Task<Result<PagedResult<CategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        try
        {
            var query = context.Categories
                .Where(c => c.IsActive && !c.IsDeleted);

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderBy(c => c.Name)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(c => new CategoryDto(
                    c.Id,
                    c.Name,
                    c.Description,
                    context.Products.Count(p => p.CategoryId == c.Id && !p.IsDeleted)))
                .ToListAsync(ct);

            return Result<PagedResult<CategoryDto>>.Success(
                new PagedResult<CategoryDto>(items, total, request.Page, request.PageSize));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<PagedResult<CategoryDto>>.Failure(CategoryErrors.UnknownError);
        }
    }
}
