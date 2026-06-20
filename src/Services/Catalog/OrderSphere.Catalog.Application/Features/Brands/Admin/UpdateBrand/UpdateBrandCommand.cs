using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Catalog.Application.Features.Brands.Admin.UpdateBrand;

public sealed record UpdateBrandCommand(BrandId BrandId, string Name, string Description, string? LogoUrl, bool IsActive)
    : ICommand<Result>;
