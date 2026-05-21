using OrderSphere.Application.Models;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Product.GetProductBySlug;

public sealed record GetProductBySlugQuery(string Slug) : IQuery<Result<ProductDto>>;
