using OrderSphere.Application.Models.Events;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Order.ProcessOrder;

public sealed record ProcessOrderCommand(CheckoutCartEvent Event)
    : ICommand<Result<Guid>>;
