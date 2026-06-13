namespace OrderSphere.Catalog.Application.Features.Products.Public.GetProducts;

public sealed record GetProductsQuery(
    int Page = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    string? CategoryName = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null)
    : IQuery<Result<PagedResult<ProductDto>>>;
