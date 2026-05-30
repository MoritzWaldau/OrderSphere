using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Catalog.Application.DTOs.Public;

namespace OrderSphere.Catalog.Application.Features.Products.Public.GetProducts;

public sealed class GetProductsQueryHandler(ICatalogDbContext context)
    : IQueryHandler<GetProductsQuery, Result<PagedResult<ProductDto>>>
{
    public async Task<Result<PagedResult<ProductDto>>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var query = context.Products
            .Include(p => p.Category)
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                (p.Description != null && p.Description.ToLower().Contains(term)) ||
                p.SKU.ToLower().Contains(term));
        }

        if (request.CategoryId.HasValue)
        {
            var catId = CategoryId.From(request.CategoryId.Value);
            query = query.Where(p => p.CategoryId == catId);
        }

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
