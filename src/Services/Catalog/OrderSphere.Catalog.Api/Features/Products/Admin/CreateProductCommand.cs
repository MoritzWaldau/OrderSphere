using MediatR;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Catalog.Api.Features.Products.Admin;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    int Stock,
    Guid CategoryId,
    string SKU) : IRequest<Result<Guid>>;
