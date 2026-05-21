using MediatR;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Catalog.Api.Models.Admin;

namespace OrderSphere.Catalog.Api.Features.Categories.Admin;

public sealed record GetAllCategoriesAdminQuery : IRequest<Result<IEnumerable<AdminCategoryDto>>>;
