using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Cart.CreateCart;

public sealed record CreateCartCommand(CartDto Cart) : ICommand<Result>;
