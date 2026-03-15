using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Checkout;

public sealed record CheckoutCartCommand(CheckoutCartDto CheckoutCartDto) 
    : ICommand<Result>;
