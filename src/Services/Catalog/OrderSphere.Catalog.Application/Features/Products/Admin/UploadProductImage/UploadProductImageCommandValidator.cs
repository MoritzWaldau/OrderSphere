namespace OrderSphere.Catalog.Application.Features.Products.Admin.UploadProductImage;

public sealed class UploadProductImageCommandValidator : AbstractValidator<UploadProductImageCommand>
{
    private static readonly string[] AllowedContentTypes =
        ["image/jpeg", "image/png", "image/webp", "image/gif"];

    public UploadProductImageCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("Only JPEG, PNG, WebP, or GIF images are accepted.");
    }
}
