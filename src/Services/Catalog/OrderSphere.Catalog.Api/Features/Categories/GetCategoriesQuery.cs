using MediatR;
using OrderSphere.Catalog.Api.Models;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Catalog.Api.Features.Categories;

public sealed record GetCategoriesQuery : IRequest<Result<IEnumerable<CategoryDto>>>;
