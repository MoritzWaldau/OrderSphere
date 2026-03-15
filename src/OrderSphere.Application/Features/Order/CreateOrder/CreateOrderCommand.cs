
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Order.CreateOrder;

public sealed record CreateOrderCommand() 
    : ICommand<Result>;

