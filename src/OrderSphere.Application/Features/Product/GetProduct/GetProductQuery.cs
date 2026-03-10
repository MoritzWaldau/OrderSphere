using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Product.GetProduct;

public sealed record GetProductQuery : IQuery<Result<IEnumerable<ProductDto>>>;
