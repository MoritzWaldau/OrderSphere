using FluentValidation;

namespace OrderSphere.Basket.Api.Features.Cart;

public sealed class DecreaseCartItemQuantityCommandValidator : AbstractValidator<DecreaseCartItemQuantityCommand>
{
    public DecreaseCartItemQuantityCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
    }
}
