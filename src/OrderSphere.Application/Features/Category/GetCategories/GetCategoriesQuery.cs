using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Category.GetCategories;

public sealed record GetCategoriesQuery() : IQuery<Result<IReadOnlyList<CategoryDto>>>;
