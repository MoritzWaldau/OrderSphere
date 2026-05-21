using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Category.Admin.UpdateCategory;

public sealed record UpdateCategoryCommand(Guid CategoryId, string Name, string Description, bool IsActive) : ICommand<Result<bool>>;
