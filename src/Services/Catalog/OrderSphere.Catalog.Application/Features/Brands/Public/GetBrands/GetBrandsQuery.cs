namespace OrderSphere.Catalog.Application.Features.Brands.Public.GetBrands;

public sealed record GetBrandsQuery(int Page = 1, int PageSize = 100)
    : IQuery<Result<PagedResult<BrandDto>>>;
