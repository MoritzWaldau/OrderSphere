using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Catalog.Api.Caching;
using OrderSphere.Catalog.Domain.Entities;
using OrderSphere.Catalog.Domain.Errors;
using OrderSphere.Catalog.Infrastructure.Persistence;

namespace OrderSphere.Catalog.Api.Features.Products.Admin;

public sealed class CreateProductCommandHandler(CatalogDbContext context, HybridCache cache)
    : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        try
        {
            var exists = await context.Products
                .AnyAsync(p => p.SKU == request.SKU && !p.IsDeleted, ct);
            if (exists)
                return Result<Guid>.Failure(ProductErrors.NameAlreadyExists);

            var product = new Product(
                request.Name, request.Description, request.Price,
                request.Stock, request.CategoryId, request.SKU);

            context.Products.Add(product);
            await context.SaveChangesAsync(ct);
            await cache.RemoveByTagAsync(CatalogCache.Tag, ct);

            return Result<Guid>.Success(product.Id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<Guid>.Failure(ProductErrors.UnknownError);
        }
    }
}
