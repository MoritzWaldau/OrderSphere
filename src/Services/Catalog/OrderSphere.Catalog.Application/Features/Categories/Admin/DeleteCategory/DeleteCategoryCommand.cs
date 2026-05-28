using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Catalog.Application.Features.Categories.Admin.DeleteCategory;

public sealed record DeleteCategoryCommand(CategoryId CategoryId) : ICommand<Result>;
