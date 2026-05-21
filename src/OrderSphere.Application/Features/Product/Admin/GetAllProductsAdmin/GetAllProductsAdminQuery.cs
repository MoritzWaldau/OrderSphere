using OrderSphere.Application.Models.Admin;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Product.Admin.GetAllProductsAdmin;

public sealed record GetAllProductsAdminQuery : IQuery<Result<IReadOnlyList<AdminProductDto>>>;
