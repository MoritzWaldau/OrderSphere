namespace OrderSphere.Catalog.Application.Features.Brands.Admin.DeleteBrand;

public sealed class DeleteBrandCommandValidator : AbstractValidator<DeleteBrandCommand>
{
    public DeleteBrandCommandValidator()
    {
        RuleFor(x => x.BrandId).NotEmpty();
    }
}
