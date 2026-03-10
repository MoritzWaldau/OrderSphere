using OrderSphere.Application.Models.Product;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Product.CreateProduct
{
    public sealed record CreateProductCommand(CreateProductRequest Request) : ICommand<Result>;
}
