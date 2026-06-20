namespace OrderSphere.Catalog.Application.Features.Brands.Admin.UpdateBrand;

public sealed class UpdateBrandCommandValidator : AbstractValidator<UpdateBrandCommand>
{
    public UpdateBrandCommandValidator()
    {
        RuleFor(x => x.BrandId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LogoUrl).MaximumLength(500);
    }
}
