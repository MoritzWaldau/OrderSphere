namespace OrderSphere.Catalog.Application.Features.Categories.Admin.GetAllCategoriesAdmin;

public sealed record GetAllCategoriesAdminQuery : IRequest<Result<IEnumerable<AdminCategoryDto>>>;
