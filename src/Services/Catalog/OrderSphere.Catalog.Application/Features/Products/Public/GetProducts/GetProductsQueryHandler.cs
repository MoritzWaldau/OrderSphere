using Microsoft.EntityFrameworkCore;
using OrderSphere.Catalog.Application.DTOs.Public;

namespace OrderSphere.Catalog.Application.Features.Products.Public.GetProducts;

public sealed class GetProductsQueryHandler(ICatalogDbContext context)
    : IRequestHandler<GetProductsQuery, Result<PagedResult<ProductDto>>>
{
    public async Task<Result<PagedResult<ProductDto>>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var query = context.Products
            .Include(p => p.Category)
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.IsActive);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductDto
            {
                Id = p.Id.Value,
                Name = p.Name,
                Slug = p.Slug,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock,
                CategoryId = p.CategoryId.Value,
                CategoryName = p.Category!.Name,
                SKU = p.SKU,
                ImageUrl = p.ImageUrl,
                IsActive = p.IsActive
            })
            .ToListAsync(ct);

        return Result<PagedResult<ProductDto>>.Success(
            new PagedResult<ProductDto>(items, total, request.Page, request.PageSize));
    }
}
