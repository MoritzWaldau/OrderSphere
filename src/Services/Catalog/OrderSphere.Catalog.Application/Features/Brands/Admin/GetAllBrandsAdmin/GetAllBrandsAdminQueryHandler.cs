namespace OrderSphere.Catalog.Application.Features.Brands.Admin.GetAllBrandsAdmin;

public sealed class GetAllBrandsAdminQueryHandler(ICatalogDbContext context)
    : IQueryHandler<GetAllBrandsAdminQuery, Result<IEnumerable<AdminBrandDto>>>
{
    public async Task<Result<IEnumerable<AdminBrandDto>>> Handle(GetAllBrandsAdminQuery request, CancellationToken ct)
    {
        try
        {
            var brands = await context.Brands
                .OrderBy(b => b.Name)
                .Select(b => new AdminBrandDto(
                    b.Id.Value, b.Name, b.Slug, b.Description, b.LogoUrl, b.IsActive,
                    context.Products.Count(p => p.BrandId == b.Id),
                    b.CreatedAt, b.UpdatedAt))
                .ToListAsync(ct);

            return Result<IEnumerable<AdminBrandDto>>.Success(brands);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<IEnumerable<AdminBrandDto>>.Failure(BrandErrors.UnknownError);
        }
    }
}
