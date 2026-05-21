using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Product.Admin.DeleteProduct;

public sealed record DeleteProductCommand(Guid ProductId) : ICommand<Result<bool>>;
