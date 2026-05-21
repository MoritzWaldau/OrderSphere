using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using OrderSphere.Catalog.Api.Caching;
using OrderSphere.Catalog.Api.Models;
using OrderSphere.Catalog.Domain.Errors;
using OrderSphere.Catalog.Infrastructure.Persistence;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Catalog.Api.Features.Products;

public sealed class GetProductsQueryHandler(
    CatalogDbContext context,
    HybridCache cache) : IRequestHandler<GetProductsQuery, Result<IEnumerable<ProductDto>>>
{
    public async Task<Result<IEnumerable<ProductDto>>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        try
        {
            var products = await cache.GetOrCreateAsync(
                CatalogCache.ProductsAllKey,
                async token => await context.Products
                    .Include(p => p.Category)
                    .Where(p => p.Stock > 0 && p.IsActive && !p.IsDeleted)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Slug = p.Slug,
                        Description = p.Description,
                        Price = p.Price,
                        Stock = p.Stock,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category!.Name,
                        SKU = p.SKU,
                        ImageUrl = p.ImageUrl,
                        IsActive = p.IsActive,
                    })
                    .ToListAsync(token),
                tags: [CatalogCache.Tag],
                cancellationToken: ct);

            return Result<IEnumerable<ProductDto>>.Success(products);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<IEnumerable<ProductDto>>.Failure(ProductErrors.UnknownError);
        }
    }
}
