using OrderSphere.Application.Models;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Product.GetProduct;

public sealed record GetProductQuery : IQuery<Result<IEnumerable<ProductDto>>>;
