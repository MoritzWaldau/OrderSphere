namespace OrderSphere.Catalog.Application.Features.Brands.Admin.DeleteBrand;

public sealed class DeleteBrandCommandHandler(ICatalogDbContext context)
    : ICommandHandler<DeleteBrandCommand, Result>
{
    public async Task<Result> Handle(DeleteBrandCommand request, CancellationToken ct)
    {
        var brand = await context.Brands
            .AsTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BrandId, ct);

        if (brand is null)
            return Result.Failure(BrandErrors.NotFound);

        var hasProducts = await context.Products
            .AsNoTracking()
            .AnyAsync(p => p.BrandId == request.BrandId, ct);

        if (hasProducts)
            return Result.Failure(BrandErrors.HasProducts);

        brand.Delete();

        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
