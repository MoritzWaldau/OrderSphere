namespace OrderSphere.Catalog.Application.Features.Products.Public.GetProducts;

public sealed record GetProductsQuery(
    int Page = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    string? CategoryName = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    ProductSortBy SortBy = ProductSortBy.Name,
    SortDirection SortDir = SortDirection.Asc)
    : IQuery<Result<PagedResult<ProductDto>>>;

public enum ProductSortBy
{
    Name,
    Price,
    Newest,
}

public enum SortDirection
{
    Asc,
    Desc,
}
