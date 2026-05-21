namespace OrderSphere.Catalog.Application.Features.Products.Admin.GetAll;

public sealed class GetAllProductsAdminQueryHandler(ICatalogDbContext context)
    : IRequestHandler<GetAllProductsAdminQuery, Result<IEnumerable<AdminProductDto>>>
{
    public async Task<Result<IEnumerable<AdminProductDto>>> Handle(GetAllProductsAdminQuery request, CancellationToken ct)
    {
        try
        {
            var products = await context.Products
                .Include(p => p.Category)
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.Name)
                .Select(p => new AdminProductDto(
                    p.Id, p.Name, p.Slug, p.Description, p.Price, p.Stock,
                    p.CategoryId, p.Category!.Name, p.SKU, p.IsActive, p.CreatedAt, p.UpdatedAt))
                .ToListAsync(ct);

            return Result<IEnumerable<AdminProductDto>>.Success(products);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<IEnumerable<AdminProductDto>>.Failure(ProductErrors.UnknownError);
        }
    }
}
