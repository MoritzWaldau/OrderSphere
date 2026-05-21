using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using OrderSphere.Catalog.Api.Caching;
using OrderSphere.Catalog.Domain.Errors;
using OrderSphere.Catalog.Infrastructure.Persistence;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Catalog.Api.Features.Categories.Admin;

public sealed class DeleteCategoryCommandHandler(CatalogDbContext context, HybridCache cache)
    : IRequestHandler<DeleteCategoryCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        try
        {
            var hasProducts = await context.Products
                .AnyAsync(p => p.CategoryId == request.CategoryId && !p.IsDeleted, ct);
            if (hasProducts)
                return Result<bool>.Failure(CategoryErrors.HasProducts);

            var category = await context.Categories
                .AsTracking()
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId && !c.IsDeleted, ct);

            if (category is null)
                return Result<bool>.Failure(CategoryErrors.NotFound);

            category.IsDeleted = true;
            await context.SaveChangesAsync(ct);
            await cache.RemoveByTagAsync(CatalogCache.Tag, ct);
            return Result<bool>.Success(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<bool>.Failure(CategoryErrors.UnknownError);
        }
    }
}
