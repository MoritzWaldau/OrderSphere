using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Category.Admin.UpdateCategory;

public sealed record UpdateCategoryCommand(Guid CategoryId, string Name, string Description, bool IsActive) : ICommand<Result<bool>>;
