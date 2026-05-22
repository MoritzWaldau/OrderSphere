using Microsoft.EntityFrameworkCore;
using OrderSphere.Catalog.Domain.Entities;
using OrderSphere.Catalog.Domain.Errors;

namespace OrderSphere.Catalog.Application.Features.Products.Admin.CreateProduct;

public sealed class CreateProductCommandHandler(ICatalogDbContext context)
    : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var skuExists = await context.Products
            .AsNoTracking()
            .AnyAsync(p => p.SKU == request.SKU && !p.IsDeleted, ct);

        if (skuExists)
            return Result<Guid>.Failure(ProductErrors.SkuAlreadyExists);

        var categoryExists = await context.Categories
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.CategoryId && !c.IsDeleted, ct);

        if (!categoryExists)
            return Result<Guid>.Failure(CategoryErrors.NotFound);

        var product = new Product(
            request.Name,
            request.Description,
            request.Price,
            request.Stock,
            request.CategoryId,
            request.SKU,
            request.ImageUrl);

        context.Products.Add(product);
        await context.SaveChangesAsync(ct);

        return Result<Guid>.Success(product.Id);
    }
}
