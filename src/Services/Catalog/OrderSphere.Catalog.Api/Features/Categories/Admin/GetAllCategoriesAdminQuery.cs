using MediatR;
using OrderSphere.Catalog.Api.Models.Admin;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Catalog.Api.Features.Categories.Admin;

public sealed record GetAllCategoriesAdminQuery : IRequest<Result<IEnumerable<AdminCategoryDto>>>;
