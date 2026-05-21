using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderSphere.Application.Features.Cart.RemoveFromCart;

public sealed record RemoveFromCartCommand(
    Guid CustomerId,
    Guid ProductId) : ICommand<Result>;
