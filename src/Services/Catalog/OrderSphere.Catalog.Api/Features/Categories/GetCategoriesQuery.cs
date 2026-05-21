using MediatR;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Catalog.Api.Models;

namespace OrderSphere.Catalog.Api.Features.Categories;

public sealed record GetCategoriesQuery : IRequest<Result<IEnumerable<CategoryDto>>>;
