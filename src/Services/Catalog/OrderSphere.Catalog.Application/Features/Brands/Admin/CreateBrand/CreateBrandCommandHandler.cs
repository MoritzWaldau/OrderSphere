using OrderSphere.Catalog.Domain.Entities;

namespace OrderSphere.Catalog.Application.Features.Brands.Admin.CreateBrand;

public sealed class CreateBrandCommandHandler(ICatalogDbContext context)
    : ICommandHandler<CreateBrandCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateBrandCommand request, CancellationToken ct)
    {
        var nameExists = await context.Brands
            .AsNoTracking()
            .AnyAsync(b => b.Name == request.Name, ct);

        if (nameExists)
            return Result<Guid>.Failure(BrandErrors.NameAlreadyExists);

        var brand = new Brand(request.Name, request.Description, request.LogoUrl);

        context.Brands.Add(brand);
        await context.SaveChangesAsync(ct);

        return Result<Guid>.Success(brand.Id.Value);
    }
}
