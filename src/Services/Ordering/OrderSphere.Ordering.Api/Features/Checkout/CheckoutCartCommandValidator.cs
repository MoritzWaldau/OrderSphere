using FluentValidation;

namespace OrderSphere.Ordering.Api.Features.Checkout;

public sealed class CheckoutCartCommandValidator : AbstractValidator<CheckoutCartCommand>
{
    public CheckoutCartCommandValidator()
    {
        RuleFor(x => x.Request.CustomerId).NotEmpty();
        RuleFor(x => x.Request.CustomerEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Request.CustomerName).NotEmpty();
        RuleFor(x => x.Request.ShippingAddress.FirstName).NotEmpty();
        RuleFor(x => x.Request.ShippingAddress.LastName).NotEmpty();
        RuleFor(x => x.Request.ShippingAddress.Street).NotEmpty();
        RuleFor(x => x.Request.ShippingAddress.City).NotEmpty();
        RuleFor(x => x.Request.ShippingAddress.PostalCode).NotEmpty();
        RuleFor(x => x.Request.ShippingAddress.Country).NotEmpty();
    }
}
