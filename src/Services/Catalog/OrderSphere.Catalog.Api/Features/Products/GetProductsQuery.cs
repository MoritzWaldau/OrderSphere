using MediatR;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Catalog.Api.Models;

namespace OrderSphere.Catalog.Api.Features.Products;

public sealed record GetProductsQuery : IRequest<Result<IEnumerable<ProductDto>>>;
