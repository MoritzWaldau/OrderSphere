using OrderSphere.Application.Models.Admin;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Category.Admin.UpdateCategory;

public sealed record UpdateCategoryCommand(Guid CategoryId, AdminCategoryInput Input, bool IsActive)
    : ICommand<Result<bool>>;
