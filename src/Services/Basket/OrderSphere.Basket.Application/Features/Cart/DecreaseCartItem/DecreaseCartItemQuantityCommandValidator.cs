using FluentValidation;

namespace OrderSphere.Basket.Application.Features.Cart.DecreaseCartItem;

public sealed class DecreaseCartItemQuantityCommandValidator : AbstractValidator<DecreaseCartItemQuantityCommand>
{
    public DecreaseCartItemQuantityCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
    }
}
