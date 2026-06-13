using Microsoft.Extensions.Caching.Hybrid;

namespace OrderSphere.Catalog.Application.Features.Products.Public.GetProductBySlug;

public sealed class GetProductBySlugQueryHandler(ICatalogDbContext context, HybridCache cache)
    : IQueryHandler<GetProductBySlugQuery, Result<ProductDto>>
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
            .Where(p => p.Slug == slug && p.IsActive)
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
            .FirstOrDefaultAsync(ct);
}
