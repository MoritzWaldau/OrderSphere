using MediatR;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Catalog.Api.Features.Products.Admin;

public sealed record DeleteProductCommand(Guid ProductId) : IRequest<Result<bool>>;
