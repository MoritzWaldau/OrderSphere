using OrderSphere.Application.Models.Admin;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Product.Admin.GetProductByIdAdmin;

public sealed record GetProductByIdAdminQuery(Guid ProductId) : IQuery<Result<AdminProductDto>>;
