using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Catalog.Application.Features.Brands.Admin.DeleteBrand;

public sealed record DeleteBrandCommand(BrandId BrandId) : ICommand<Result>;
