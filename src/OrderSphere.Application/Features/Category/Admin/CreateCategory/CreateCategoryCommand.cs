using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Category.Admin.CreateCategory;

public sealed record CreateCategoryCommand(string Name, string Description) : ICommand<Result<Guid>>;
