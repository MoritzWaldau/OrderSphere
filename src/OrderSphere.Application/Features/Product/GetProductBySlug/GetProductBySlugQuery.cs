using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Product.GetProductBySlug;

public sealed record GetProductBySlugQuery(string Slug) : IQuery<Result<ProductDto>>;
