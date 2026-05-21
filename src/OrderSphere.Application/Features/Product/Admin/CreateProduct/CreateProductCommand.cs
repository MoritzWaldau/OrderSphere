using OrderSphere.Application.Models.Admin;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Product.Admin.CreateProduct;

public sealed record CreateProductCommand(AdminProductInput Input) : ICommand<Result<Guid>>;
