namespace OrderSphere.Catalog.Application.Features.Brands.Admin.UpdateBrand;

public sealed class UpdateBrandCommandHandler(ICatalogDbContext context)
    : ICommandHandler<UpdateBrandCommand, Result>
{
    public async Task<Result> Handle(UpdateBrandCommand request, CancellationToken ct)
    {
        var brand = await context.Brands
            .AsTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BrandId, ct);

        if (brand is null)
            return Result.Failure(BrandErrors.NotFound);

        brand.UpdateDetails(request.Name, request.Description, request.LogoUrl);

        if (request.IsActive)
            brand.Activate();
        else
            brand.Deactivate();

        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
