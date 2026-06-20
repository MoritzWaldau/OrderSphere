namespace OrderSphere.Catalog.Application.Features.Brands.Admin.GetAllBrandsAdmin;

public sealed record GetAllBrandsAdminQuery : IQuery<Result<IEnumerable<AdminBrandDto>>>;
