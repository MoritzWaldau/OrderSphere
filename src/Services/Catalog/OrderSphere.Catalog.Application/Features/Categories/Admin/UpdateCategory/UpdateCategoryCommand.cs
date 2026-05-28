using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Catalog.Application.Features.Categories.Admin.UpdateCategory;

public sealed record UpdateCategoryCommand(CategoryId CategoryId, string Name, string Description, bool IsActive)
    : IRequest<Result>;
