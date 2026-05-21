using OrderSphere.Application.Models.Admin;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Product.Admin.CreateProduct;

public sealed record CreateProductCommand(AdminProductInput Input) : ICommand<Result<Guid>>;
