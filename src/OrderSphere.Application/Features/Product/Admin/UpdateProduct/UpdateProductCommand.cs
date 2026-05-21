using OrderSphere.Application.Models.Admin;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Product.Admin.UpdateProduct;

public sealed record UpdateProductCommand(Guid ProductId, AdminProductInput Input, bool IsActive) : ICommand<Result<bool>>;
