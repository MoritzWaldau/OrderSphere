namespace OrderSphere.Catalog.Application.Features.Categories.Admin.GetAll;

public sealed record GetAllCategoriesAdminQuery : IRequest<Result<IEnumerable<AdminCategoryDto>>>;
