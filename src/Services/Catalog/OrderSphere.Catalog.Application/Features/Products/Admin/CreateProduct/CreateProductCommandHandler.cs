using OrderSphere.BuildingBlocks.ValueObjects;
using OrderSphere.Catalog.Domain.Entities;

namespace OrderSphere.Catalog.Application.Features.Products.Admin.CreateProduct;

public sealed class CreateProductCommandHandler(ICatalogDbContext context, IProductSearchIndex searchIndex)
    : ICommandHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var skuExists = await context.Products
            .AsNoTracking()
            .AnyAsync(p => p.SKU == request.SKU, ct);

        if (skuExists)
            return Result<Guid>.Failure(ProductErrors.SkuAlreadyExists);

        var categoryExists = await context.Categories
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.CategoryId, ct);

        if (!categoryExists)
            return Result<Guid>.Failure(CategoryErrors.NotFound);

        var product = new Product(
            request.Name,
            request.Description,
            Money.Of(request.Price),
            request.Stock,
            request.CategoryId,
            request.SKU,
            request.ImageUrl);

        context.Products.Add(product);
        await context.SaveChangesAsync(ct);

        await searchIndex.SyncAsync(product.Id.Value, ct);

        // Return the raw Guid so the endpoint can use it in the Location header.
        return Result<Guid>.Success(product.Id.Value);
    }
}
