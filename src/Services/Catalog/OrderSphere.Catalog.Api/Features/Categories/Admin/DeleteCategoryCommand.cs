using MediatR;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Catalog.Api.Features.Categories.Admin;

public sealed record DeleteCategoryCommand(Guid CategoryId, string Name) : IRequest<Result<bool>>;
