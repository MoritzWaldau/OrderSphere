using FluentValidation;

namespace OrderSphere.Ordering.Application.Features.Order.Admin;

public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}
