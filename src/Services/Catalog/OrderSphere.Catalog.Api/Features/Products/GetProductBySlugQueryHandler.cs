using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Catalog.Api.Caching;
using OrderSphere.Catalog.Api.Models;
using OrderSphere.Catalog.Domain.Errors;
using OrderSphere.Catalog.Infrastructure.Persistence;

namespace OrderSphere.Catalog.Api.Features.Products;

public sealed class GetProductBySlugQueryHandler(
    CatalogDbContext context,
    HybridCache cache) : IRequestHandler<GetProductBySlugQuery, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(GetProductBySlugQuery request, CancellationToken ct)
    {
        try
        {
            var product = await cache.GetOrCreateAsync(
                CatalogCache.ProductBySlugKey(request.Slug),
                async token => await context.Products
                    .Include(p => p.Category)
                    .Where(p => p.Slug == request.Slug && !p.IsDeleted)
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
                    .FirstOrDefaultAsync(token),
                tags: [CatalogCache.Tag],
                cancellationToken: ct);

            if (product is null)
                return Result<ProductDto>.Failure(ProductErrors.NotFound);

            return Result<ProductDto>.Success(product);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<ProductDto>.Failure(ProductErrors.UnknownError);
        }
    }
}
