namespace OrderSphere.Catalog.Application.Features.Products.Admin.DeleteProduct;

public sealed class DeleteProductCommandHandler(ICatalogDbContext context)
    : ICommandHandler<DeleteProductCommand, Result>
{
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        var product = await context.Products
            .AsTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && !p.IsDeleted, ct);

        if (product is null)
            return Result.Failure(ProductErrors.NotFound);

        product.Delete();

        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
