using OrderSphere.Application.Models.Admin;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Product.Admin.GetProductByIdAdmin;

public sealed record GetProductByIdAdminQuery(Guid ProductId)
    : IQuery<Result<AdminProductDto>>;
