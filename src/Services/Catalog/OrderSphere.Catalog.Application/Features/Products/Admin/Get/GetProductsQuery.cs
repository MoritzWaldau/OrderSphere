namespace OrderSphere.Catalog.Application.Features.Products.Admin.Get;

public sealed record GetProductsQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<ProductDto>>>;
