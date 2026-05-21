using MediatR;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Ordering.Api.Models;

namespace OrderSphere.Ordering.Api.Features.Checkout;

public sealed record CheckoutCartCommand(CheckoutRequest Request) : IRequest<Result<Guid>>;
