using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Category.Admin.DeleteCategory;

public sealed record DeleteCategoryCommand(Guid CategoryId, string Name) : ICommand<Result<bool>>;
