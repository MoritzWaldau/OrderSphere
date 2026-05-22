namespace OrderSphere.Catalog.Application.Features.Categories.Admin.UpdateCategory;

public sealed record UpdateCategoryCommand(Guid CategoryId, string Name, string Description, bool IsActive)
    : IRequest<Result<bool>>;
