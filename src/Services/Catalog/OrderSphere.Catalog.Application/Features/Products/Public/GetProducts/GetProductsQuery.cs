namespace OrderSphere.Catalog.Application.Features.Products.Public.GetProducts;

public sealed record GetProductsQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<ProductDto>>>;
