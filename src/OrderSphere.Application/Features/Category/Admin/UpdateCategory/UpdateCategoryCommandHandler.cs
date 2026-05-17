using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Caching;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Category.Admin.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(
    IDbContext context,
    HybridCache cache,
    ILogger<UpdateCategoryCommandHandler> logger
) : ICommandHandler<UpdateCategoryCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var category = await context.Categories
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId && !c.IsDeleted, cancellationToken);

            if (category is null)
                return Result<bool>.Failure(CategoryErrors.NotFound);

            category.UpdateDetails(request.Input.Name, request.Input.Description);

            if (request.IsActive && !category.IsActive)
                category.Activate();
            else if (!request.IsActive && category.IsActive)
                category.Deactivate();

            context.Categories.Update(category);
            await context.BeginTransactionAsync(cancellationToken);
            await context.CommitAsync(cancellationToken);

            await cache.RemoveByTagAsync(CatalogCache.Tag, cancellationToken);

            logger.LogInformation("Category {CategoryId} updated", category.Id);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await context.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Failed to update category {CategoryId}", request.CategoryId);
            return Result<bool>.Failure(CategoryErrors.UnknownError);
        }
    }
}
