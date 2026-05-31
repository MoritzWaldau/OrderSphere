namespace OrderSphere.Catalog.Application.Features.Products.Public.GetProductBySlug;

public sealed record GetProductBySlugQuery(string Slug) : IQuery<Result<ProductDto>>;
