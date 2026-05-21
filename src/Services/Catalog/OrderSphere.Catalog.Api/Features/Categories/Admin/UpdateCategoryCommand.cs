using MediatR;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Catalog.Api.Features.Categories.Admin;

public sealed record UpdateCategoryCommand(Guid CategoryId, string Name, string Description, bool IsActive) : IRequest<Result<bool>>;
