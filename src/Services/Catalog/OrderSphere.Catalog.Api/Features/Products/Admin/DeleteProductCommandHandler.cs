using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Catalog.Api.Caching;
using OrderSphere.Catalog.Domain.Errors;
using OrderSphere.Catalog.Infrastructure.Persistence;

namespace OrderSphere.Catalog.Api.Features.Products.Admin;

public sealed class DeleteProductCommandHandler(CatalogDbContext context, HybridCache cache)
    : IRequestHandler<DeleteProductCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        try
        {
            var product = await context.Products
                .AsTracking()
                .FirstOrDefaultAsync(p => p.Id == request.ProductId && !p.IsDeleted, ct);

            if (product is null)
                return Result<bool>.Failure(ProductErrors.NotFound);

            product.IsDeleted = true;
            product.Deactivate();
            await context.SaveChangesAsync(ct);
            await cache.RemoveByTagAsync(CatalogCache.Tag, ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<bool>.Failure(ProductErrors.UnknownError);
        }
    }
}
