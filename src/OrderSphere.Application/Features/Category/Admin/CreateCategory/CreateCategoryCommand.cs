using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Category.Admin.CreateCategory;

public sealed record CreateCategoryCommand(string Name, string Description) : ICommand<Result<Guid>>;
