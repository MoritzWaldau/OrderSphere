namespace OrderSphere.Catalog.Application.Features.Categories.Admin.Update;

public sealed record UpdateCategoryCommand(Guid CategoryId, string Name, string Description, bool IsActive) 
    : IRequest<Result<bool>>;
