using OrderSphere.Application.Models.Admin;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Category.Admin.GetAllCategoriesAdmin;

public sealed record GetAllCategoriesAdminQuery : IQuery<Result<IReadOnlyList<AdminCategoryDto>>>;
