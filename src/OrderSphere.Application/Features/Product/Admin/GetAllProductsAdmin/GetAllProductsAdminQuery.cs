using OrderSphere.Application.Models.Admin;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Product.Admin.GetAllProductsAdmin;

public sealed record GetAllProductsAdminQuery : IQuery<Result<IReadOnlyList<AdminProductDto>>>;
