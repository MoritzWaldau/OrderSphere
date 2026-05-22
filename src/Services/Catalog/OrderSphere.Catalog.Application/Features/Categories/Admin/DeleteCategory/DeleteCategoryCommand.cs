namespace OrderSphere.Catalog.Application.Features.Categories.Admin.DeleteCategory;

public sealed record DeleteCategoryCommand(Guid CategoryId) : IRequest<Result<bool>>;
