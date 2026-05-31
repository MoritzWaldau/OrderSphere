namespace OrderSphere.Catalog.Application.Features.Categories.Admin.CreateCategory;

public sealed record CreateCategoryCommand(string Name, string Description) : ICommand<Result<Guid>>;
