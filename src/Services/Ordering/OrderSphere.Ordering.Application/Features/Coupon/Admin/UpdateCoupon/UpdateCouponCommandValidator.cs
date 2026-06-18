using FluentValidation;

namespace OrderSphere.Ordering.Application.Features.Coupon.Admin.UpdateCoupon;

public sealed class UpdateCouponCommandValidator : AbstractValidator<UpdateCouponCommand>
{
    public UpdateCouponCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.DiscountType).InclusiveBetween(0, 1);
        RuleFor(x => x.Value).GreaterThan(0);
        RuleFor(x => x.Value).LessThanOrEqualTo(100)
            .When(x => x.DiscountType == 1)
            .WithMessage("Ein prozentualer Rabatt muss zwischen 0 und 100 liegen.");
        RuleFor(x => x.MinSubtotal).GreaterThanOrEqualTo(0).When(x => x.MinSubtotal.HasValue);
        RuleFor(x => x.MaxRedemptions).GreaterThan(0).When(x => x.MaxRedemptions.HasValue);
        RuleFor(x => x.ValidUntil).GreaterThan(x => x.ValidFrom!.Value)
            .When(x => x.ValidFrom.HasValue && x.ValidUntil.HasValue)
            .WithMessage("Das Enddatum muss nach dem Startdatum liegen.");
    }
}
