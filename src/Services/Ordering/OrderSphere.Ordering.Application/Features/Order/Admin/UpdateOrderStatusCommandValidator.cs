using FluentValidation;

namespace OrderSphere.Ordering.Application.Features.Order.Admin;

public sealed class UpdateOrderStatusCommandValidator : AbstractValidator<UpdateOrderStatusCommand>
{
    public UpdateOrderStatusCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}
