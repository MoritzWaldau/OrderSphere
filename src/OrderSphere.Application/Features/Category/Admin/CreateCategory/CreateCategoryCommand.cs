using OrderSphere.Application.Models.Admin;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Category.Admin.CreateCategory;

public sealed record CreateCategoryCommand(AdminCategoryInput Input)
    : ICommand<Result<Guid>>;
