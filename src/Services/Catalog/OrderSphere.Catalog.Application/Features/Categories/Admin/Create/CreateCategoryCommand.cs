using MediatR;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Catalog.Application.Features.Categories.Admin.Create;

public sealed record CreateCategoryCommand(string Name, string Description) : IRequest<Result<Guid>>;
