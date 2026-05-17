using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Caching;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Product.GetProductBySlug;

public sealed class GetProductBySlugQueryHandler(
    IDbContext context,
    HybridCache cache,
    ILogger<GetProductBySlugQueryHandler> logger
    ) : IQueryHandler<GetProductBySlugQuery, Result<ProductDto>>
{
    private static readonly string[] Tags = [CatalogCache.Tag];

    public async Task<Result<ProductDto>> Handle(GetProductBySlugQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var productDto = await cache.GetOrCreateAsync<ProductDto?>(
                CatalogCache.ProductBySlugKey(request.Slug),
                async ct =>
                {
                    var product = await context.Products
                        .Include(p => p.Category)
                        .Where(p => p.Stock > 0 && p.IsActive)
                        .FirstOrDefaultAsync(p => p.Slug == request.Slug, ct);

                    if (product is null)
                        return null;

                    return new ProductDto
                    {
                        Id = product.Id,
                        Name = product.Name,
                        Slug = product.Slug,
                        Description = product.Description,
                        Price = product.Price,
                        Stock = product.Stock,
                        CategoryId = product.CategoryId,
                        CategoryName = product.Category!.Name,
                        SKU = product.SKU,
                        ImageUrl = product.ImageUrl,
                        IsActive = product.IsActive,
                    };
                },
                tags: Tags,
                cancellationToken: cancellationToken);

            if (productDto is null)
                return Result<ProductDto>.Failure(ProductErrors.ProductNotFoundError);

            return Result<ProductDto>.Success(productDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving products.");
            return Result<ProductDto>.Failure(ProductErrors.UnknownError);
        }
    }
}
