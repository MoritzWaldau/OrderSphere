namespace OrderSphere.Catalog.Application.Features.Products.Admin.Get;

public sealed record GetProductBySlugQuery(string Slug) : IRequest<Result<ProductDto>>;
