using MediatR;
using OrderSphere.Catalog.Api.Models;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Catalog.Api.Features.Products;

public sealed record GetProductsQuery : IRequest<Result<IEnumerable<ProductDto>>>;
