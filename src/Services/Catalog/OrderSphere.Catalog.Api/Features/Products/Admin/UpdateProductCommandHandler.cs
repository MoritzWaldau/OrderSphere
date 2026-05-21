using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using OrderSphere.Catalog.Api.Caching;
using OrderSphere.Catalog.Domain.Errors;
using OrderSphere.Catalog.Infrastructure.Persistence;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Catalog.Api.Features.Products.Admin;

public sealed class UpdateProductCommandHandler(CatalogDbContext context, HybridCache cache)
    : IRequestHandler<UpdateProductCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        try
        {
            var product = await context.Products
                .AsTracking()
                .FirstOrDefaultAsync(p => p.Id == request.ProductId && !p.IsDeleted, ct);

            if (product is null)
                return Result<bool>.Failure(ProductErrors.NotFound);

            product.UpdateDetails(request.Name, request.Description, request.Price,
                request.Stock, request.CategoryId, request.SKU);

            if (request.IsActive) product.Activate(); else product.Deactivate();

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
