using FluentValidation;

namespace OrderSphere.Ordering.Application.Features.Coupon.Admin.CreateCoupon;

public sealed class CreateCouponCommandValidator : AbstractValidator<CreateCouponCommand>
{
    public CreateCouponCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.DiscountType).InclusiveBetween(0, 2);
        RuleFor(x => x.Value).GreaterThan(0)
            .When(x => x.DiscountType != 2); // Tiered: value is carried per tier
        RuleFor(x => x.Value).LessThanOrEqualTo(100)
            .When(x => x.DiscountType == 1)
            .WithMessage("Ein prozentualer Rabatt muss zwischen 0 und 100 liegen.");
        RuleFor(x => x.MinSubtotal).GreaterThanOrEqualTo(0).When(x => x.MinSubtotal.HasValue);
        RuleFor(x => x.MaxRedemptions).GreaterThan(0).When(x => x.MaxRedemptions.HasValue);
        RuleFor(x => x.ValidUntil).GreaterThan(x => x.ValidFrom!.Value)
            .When(x => x.ValidFrom.HasValue && x.ValidUntil.HasValue)
            .WithMessage("Das Enddatum muss nach dem Startdatum liegen.");

        // Tiered coupons must have at least one tier with valid values.
        When(x => x.DiscountType == 2, () =>
        {
            RuleFor(x => x.Tiers).NotNull().NotEmpty()
                .WithMessage("Ein gestaffelter Coupon benötigt mindestens eine Stufe.");
            RuleForEach(x => x.Tiers).ChildRules(tier =>
            {
                tier.RuleFor(t => t.MinSubtotal).GreaterThanOrEqualTo(0);
                tier.RuleFor(t => t.DiscountValue).GreaterThan(0);
            });
        });
    }
}
