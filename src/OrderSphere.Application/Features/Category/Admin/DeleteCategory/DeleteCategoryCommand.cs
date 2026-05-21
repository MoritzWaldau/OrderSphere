using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Category.Admin.DeleteCategory;

public sealed record DeleteCategoryCommand(Guid CategoryId, string Name) : ICommand<Result<bool>>;
