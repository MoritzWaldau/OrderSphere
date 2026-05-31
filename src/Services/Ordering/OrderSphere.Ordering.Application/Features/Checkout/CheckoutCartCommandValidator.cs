using FluentValidation;

namespace OrderSphere.Ordering.Application.Features.Checkout;

public sealed class CheckoutCartCommandValidator : AbstractValidator<CheckoutCartCommand>
{
    public CheckoutCartCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.CustomerName).NotEmpty();
        RuleFor(x => x.ShippingAddress).NotNull();
        RuleFor(x => x.ShippingAddress.FirstName).NotEmpty().When(x => x.ShippingAddress is not null);
        RuleFor(x => x.ShippingAddress.LastName).NotEmpty().When(x => x.ShippingAddress is not null);
        RuleFor(x => x.ShippingAddress.Street).NotEmpty().When(x => x.ShippingAddress is not null);
        RuleFor(x => x.ShippingAddress.City).NotEmpty().When(x => x.ShippingAddress is not null);
        RuleFor(x => x.ShippingAddress.PostalCode).NotEmpty().When(x => x.ShippingAddress is not null);
        RuleFor(x => x.ShippingAddress.Country).NotEmpty().When(x => x.ShippingAddress is not null);
    }
}
