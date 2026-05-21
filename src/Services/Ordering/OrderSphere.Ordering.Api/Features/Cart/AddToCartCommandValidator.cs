using FluentValidation;

namespace OrderSphere.Ordering.Api.Features.Cart;

public sealed class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(1);
    }
}
