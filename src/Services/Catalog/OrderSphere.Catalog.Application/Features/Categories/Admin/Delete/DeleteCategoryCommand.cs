namespace OrderSphere.Catalog.Application.Features.Categories.Admin.Delete;

public sealed record DeleteCategoryCommand(Guid CategoryId, string Name) : IRequest<Result<bool>>;
