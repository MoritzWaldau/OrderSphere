using OrderSphere.Application.Models;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Checkout;

public sealed record CheckoutCartCommand(CheckoutCartDto CheckoutCartDto)
    : ICommand<Result<Guid>>;
