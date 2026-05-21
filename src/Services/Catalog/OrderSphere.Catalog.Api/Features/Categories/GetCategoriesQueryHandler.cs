using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using OrderSphere.Catalog.Api.Caching;
using OrderSphere.Catalog.Api.Models;
using OrderSphere.Catalog.Domain.Errors;
using OrderSphere.Catalog.Infrastructure.Persistence;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Catalog.Api.Features.Categories;

public sealed class GetCategoriesQueryHandler(CatalogDbContext context, HybridCache cache)
    : IRequestHandler<GetCategoriesQuery, Result<IEnumerable<CategoryDto>>>
{
    public async Task<Result<IEnumerable<CategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        try
        {
            var categories = await cache.GetOrCreateAsync(
                CatalogCache.CategoriesAllKey,
                async token => await context.Categories
                    .Where(c => c.IsActive && !c.IsDeleted)
                    .Select(c => new CategoryDto(
                        c.Id,
                        c.Name,
                        c.Description,
                        context.Products.Count(p => p.CategoryId == c.Id && !p.IsDeleted)))
                    .ToListAsync(token),
                tags: [CatalogCache.Tag],
                cancellationToken: ct);

            return Result<IEnumerable<CategoryDto>>.Success(categories);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<IEnumerable<CategoryDto>>.Failure(CategoryErrors.UnknownError);
        }
    }
}
