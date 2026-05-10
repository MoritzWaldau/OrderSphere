using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Caching;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Product.GetProduct;

public sealed class GetProductQueryHandler(
    IDbContext context,
    HybridCache cache,
    ILogger<GetProductQueryHandler> logger) : IQueryHandler<GetProductQuery, Result<IEnumerable<ProductDto>>>
{
    private static readonly string[] Tags = [CatalogCache.Tag];

    public async Task<Result<IEnumerable<ProductDto>>> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var products = await cache.GetOrCreateAsync(
                CatalogCache.ProductsAllKey,
                async ct => await context.Products
                    .Include(p => p.Category)
                    .Where(p => p.Stock > 0 && p.IsActive)
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
                        IsActive = p.IsActive,
                    })
                    .ToListAsync(ct),
                tags: Tags,
                cancellationToken: cancellationToken);

            return Result<IEnumerable<ProductDto>>.Success(products);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving products.");
            return Result<IEnumerable<ProductDto>>.Failure(ProductErrors.UnknownError);
        }
    }
}
