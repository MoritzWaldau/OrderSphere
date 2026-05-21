using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Catalog.Api.Caching;
using OrderSphere.Catalog.Domain.Errors;
using OrderSphere.Catalog.Infrastructure.Persistence;

namespace OrderSphere.Catalog.Api.Features.Categories.Admin;

public sealed class UpdateCategoryCommandHandler(CatalogDbContext context, HybridCache cache)
    : IRequestHandler<UpdateCategoryCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        try
        {
            var category = await context.Categories
                .AsTracking()
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId && !c.IsDeleted, ct);

            if (category is null)
                return Result<bool>.Failure(CategoryErrors.NotFound);

            category.UpdateDetails(request.Name, request.Description);
            if (request.IsActive) category.Activate(); else category.Deactivate();

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
