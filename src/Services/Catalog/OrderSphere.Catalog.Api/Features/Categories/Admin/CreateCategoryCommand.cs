using MediatR;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Catalog.Api.Features.Categories.Admin;

public sealed record CreateCategoryCommand(string Name, string Description) : IRequest<Result<Guid>>;
