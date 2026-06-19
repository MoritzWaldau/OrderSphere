using OrderSphere.BuildingBlocks.ValueObjects;

namespace OrderSphere.Catalog.Application.Features.Products.Admin.UpdateProduct;

public sealed class UpdateProductCommandHandler(ICatalogDbContext context, IProductSearchIndex searchIndex)
    : ICommandHandler<UpdateProductCommand, Result>
{
    public async Task<Result> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await context.Products
            .AsTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, ct);

        if (product is null)
            return Result.Failure(ProductErrors.NotFound);

        product.UpdateDetails(
            request.Name,
            request.Description,
            Money.Of(request.Price),
            request.Stock,
            request.CategoryId,
            request.SKU,
            request.ImageUrl);

        if (request.IsActive)
            product.Activate();
        else
            product.Deactivate();

        await context.SaveChangesAsync(ct);

        await searchIndex.SyncAsync(product.Id.Value, ct);

        return Result.Success();
    }
}
