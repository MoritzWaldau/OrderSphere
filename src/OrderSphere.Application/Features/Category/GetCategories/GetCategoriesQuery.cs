using OrderSphere.Application.Models;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Category.GetCategories;

public sealed record GetCategoriesQuery : IQuery<Result<IEnumerable<CategoryDto>>>;
