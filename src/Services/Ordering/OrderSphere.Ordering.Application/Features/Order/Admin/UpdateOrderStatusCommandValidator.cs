using FluentValidation;
using OrderSphere.Ordering.Domain.Enums;

namespace OrderSphere.Ordering.Application.Features.Order.Admin;

public sealed class UpdateOrderStatusCommandValidator : AbstractValidator<UpdateOrderStatusCommand>
{
    public UpdateOrderStatusCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();

        RuleFor(x => x.NewStatus)
            .IsInEnum()
            .Must(s => s is OrderStatus.Shipped or OrderStatus.Delivered)
            .WithMessage("NewStatus must be Shipped or Delivered. Use the cancel endpoint to cancel an order.");
    }
}
