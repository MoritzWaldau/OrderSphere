using OrderSphere.Application.Models.Admin;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Category.Admin.GetAllCategoriesAdmin;

public sealed record GetAllCategoriesAdminQuery : IQuery<Result<IReadOnlyList<AdminCategoryDto>>>;
