using MediatR;
using OrderSphere.Domain.Primitives;
using OrderSphere.Ordering.Api.Models;

namespace OrderSphere.Ordering.Api.Features.Checkout;

public sealed record CheckoutCartCommand(CheckoutRequest Request) : IRequest<Result<Guid>>;
