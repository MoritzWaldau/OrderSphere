namespace OrderSphere.Catalog.Application.Features.Brands.Public.GetBrands;

public sealed class GetBrandsQueryHandler(ICatalogDbContext context)
    : IQueryHandler<GetBrandsQuery, Result<PagedResult<BrandDto>>>
{
    public async Task<Result<PagedResult<BrandDto>>> Handle(GetBrandsQuery request, CancellationToken ct)
    {
        try
        {
            var query = context.Brands
                .Where(b => b.IsActive);

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderBy(b => b.Name)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(b => new BrandDto(
                    b.Id.Value,
                    b.Name,
                    b.Slug,
                    b.Description,
                    b.LogoUrl,
                    context.Products.Count(p => p.BrandId == b.Id)))
                .ToListAsync(ct);

            return Result<PagedResult<BrandDto>>.Success(
                new PagedResult<BrandDto>(items, total, request.Page, request.PageSize));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<PagedResult<BrandDto>>.Failure(BrandErrors.UnknownError);
        }
    }
}
