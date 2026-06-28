using FluentValidation;

namespace OrderSphere.Ordering.Application.Features.Returns.RequestReturn;

public sealed class RequestReturnCommandValidator : AbstractValidator<RequestReturnCommand>
{
    public RequestReturnCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}
