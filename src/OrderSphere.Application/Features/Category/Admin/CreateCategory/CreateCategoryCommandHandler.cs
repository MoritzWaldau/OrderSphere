using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Caching;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Category.Admin.CreateCategory;

public sealed class CreateCategoryCommandHandler(
    IDbContext context,
    HybridCache cache,
    ILogger<CreateCategoryCommandHandler> logger
) : ICommandHandler<CreateCategoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var category = new Domain.Entities.Category(request.Input.Name, request.Input.Description);

            await context.BeginTransactionAsync(cancellationToken);
            await context.Categories.AddAsync(category, cancellationToken);
            await context.CommitAsync(cancellationToken);

            await cache.RemoveByTagAsync(CatalogCache.Tag, cancellationToken);

            logger.LogInformation("Category {CategoryId} '{Name}' created", category.Id, category.Name);
            return Result<Guid>.Success(category.Id);
        }
        catch (Exception ex)
        {
            await context.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Failed to create category '{Name}'", request.Input.Name);
            return Result<Guid>.Failure(CategoryErrors.UnknownError);
        }
    }
}
