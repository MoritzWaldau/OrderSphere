using FluentValidation;

namespace OrderSphere.Ordering.Api.Features.Order.Admin;

public sealed class UpdateOrderStatusCommandValidator : AbstractValidator<UpdateOrderStatusCommand>
{
    public UpdateOrderStatusCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}
