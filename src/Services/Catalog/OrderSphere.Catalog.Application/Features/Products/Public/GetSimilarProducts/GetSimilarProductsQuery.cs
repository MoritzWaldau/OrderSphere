namespace OrderSphere.Catalog.Application.Features.Products.Public.GetSimilarProducts;

public sealed record GetSimilarProductsQuery(string Slug, int Limit = 5)
    : IQuery<Result<IReadOnlyList<ProductDto>>>;
