namespace OrderSphere.Catalog.Application.Features.Categories.Admin.GetAllCategoriesAdmin;

public sealed record GetAllCategoriesAdminQuery : IQuery<Result<IEnumerable<AdminCategoryDto>>>;
