using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Category.Admin.DeleteCategory;

public sealed class DeleteCategoryCommandHandler(
    IDbContext context,
    ILogger<DeleteCategoryCommandHandler> logger
) : ICommandHandler<DeleteCategoryCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var category = await context.Categories
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId && !c.IsDeleted, cancellationToken);

            if (category is null)
                return Result<bool>.Failure(CategoryErrors.NotFound);

            // Refuse delete if any active product references this category
            var hasProducts = await context.Products
                .AnyAsync(p => p.CategoryId == request.CategoryId && !p.IsDeleted, cancellationToken);

            if (hasProducts)
            {
                logger.LogWarning("Refused to delete category {CategoryId}: still has products", request.CategoryId);
                return Result<bool>.Failure(CategoryErrors.HasProducts);
            }

            category.IsDeleted = true;
            category.Deactivate();
            context.Categories.Update(category);

            await context.BeginTransactionAsync(cancellationToken);
            await context.CommitAsync(cancellationToken);

            logger.LogInformation("Category {CategoryId} soft-deleted", category.Id);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await context.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Failed to delete category {CategoryId}", request.CategoryId);
            return Result<bool>.Failure(CategoryErrors.UnknownError);
        }
    }
}
