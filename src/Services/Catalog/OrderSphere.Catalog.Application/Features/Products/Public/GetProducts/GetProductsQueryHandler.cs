namespace OrderSphere.Catalog.Application.Features.Products.Public.GetProducts;

public sealed class GetProductsQueryHandler(ICatalogDbContext context)
    : IQueryHandler<GetProductsQuery, Result<PagedResult<ProductDto>>>
{
    public async Task<Result<PagedResult<ProductDto>>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var query = context.Products
            .Include(p => p.Category)
            .AsNoTracking()
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.Description.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.CategoryName))
        {
            var category = request.CategoryName.Trim().ToLower();
            query = query.Where(p => p.Category!.Name.ToLower() == category);
        }

        if (request.MinPrice is { } minPrice)
        {
            query = query.Where(p => p.Price.Amount >= minPrice);
        }

        if (request.MaxPrice is { } maxPrice)
        {
            query = query.Where(p => p.Price.Amount <= maxPrice);
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductDto(
                p.Id.Value,
                p.Name,
                p.Slug,
                p.Description,
                p.Price,
                p.Stock,
                p.CategoryId.Value,
                p.Category!.Name,
                p.SKU,
                p.ImageUrl,
                p.IsActive))
            .ToListAsync(ct);

        return Result<PagedResult<ProductDto>>.Success(
            new PagedResult<ProductDto>(items, total, request.Page, request.PageSize));
    }
}
