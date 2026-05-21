using MediatR;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Catalog.Api.Features.Categories.Admin;

public sealed record DeleteCategoryCommand(Guid CategoryId, string Name) : IRequest<Result<bool>>;
