using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Product.Admin.DeleteProduct;

public sealed record DeleteProductCommand(Guid ProductId)
    : ICommand<Result<bool>>;
