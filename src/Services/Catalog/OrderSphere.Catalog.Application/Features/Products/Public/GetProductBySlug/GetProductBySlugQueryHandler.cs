using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using OrderSphere.Catalog.Application.Caching;
using OrderSphere.Catalog.Application.DTOs.Public;
using OrderSphere.Catalog.Domain.Errors;

namespace OrderSphere.Catalog.Application.Features.Products.Public.GetProductBySlug;

public sealed class GetProductBySlugQueryHandler(ICatalogDbContext context, HybridCache cache)
    : IRequestHandler<GetProductBySlugQuery, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(GetProductBySlugQuery request, CancellationToken ct)
    {
        var dto = await cache.GetOrCreateAsync(
            CatalogCache.ProductBySlugKey(request.Slug),
            async token => await FetchAsync(request.Slug, token),
            tags: [CatalogCache.Tag],
            cancellationToken: ct);

        return dto is null
            ? Result<ProductDto>.Failure(ProductErrors.NotFound)
            : Result<ProductDto>.Success(dto);
    }

    private async Task<ProductDto?> FetchAsync(string slug, CancellationToken ct) =>
        await context.Products
            .Include(p => p.Category)
            .AsNoTracking()
            .Where(p => p.Slug == slug && !p.IsDeleted && p.IsActive)
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
            .FirstOrDefaultAsync(ct);
}
