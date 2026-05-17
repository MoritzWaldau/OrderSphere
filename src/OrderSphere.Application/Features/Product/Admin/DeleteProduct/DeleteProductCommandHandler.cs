using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Caching;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Product.Admin.DeleteProduct;

public sealed class DeleteProductCommandHandler(
    IDbContext context,
    HybridCache cache,
    ILogger<DeleteProductCommandHandler> logger
) : ICommandHandler<DeleteProductCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var product = await context.Products
                .FirstOrDefaultAsync(p => p.Id == request.ProductId && !p.IsDeleted, cancellationToken);

            if (product is null)
                return Result<bool>.Failure(ProductErrors.ProductNotFoundError);

            // Soft delete
            product.IsDeleted = true;
            product.Deactivate();
            context.Products.Update(product);

            await context.BeginTransactionAsync(cancellationToken);
            await context.CommitAsync(cancellationToken);

            await cache.RemoveByTagAsync(CatalogCache.Tag, cancellationToken);

            logger.LogInformation("Product {ProductId} soft-deleted", product.Id);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await context.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Failed to delete product {ProductId}", request.ProductId);
            return Result<bool>.Failure(ProductErrors.UnknownError);
        }
    }
}
