namespace OrderSphere.Catalog.Application.Features.Products.Admin.UploadProductImage;

public sealed class UploadProductImageCommandHandler(
    ICatalogDbContext context,
    IBlobStorageService blobService,
    IProductSearchIndex searchIndex)
    : ICommandHandler<UploadProductImageCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UploadProductImageCommand request, CancellationToken ct)
    {
        var product = await context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, ct);

        if (product is null)
            return Result<string>.Failure(ProductErrors.NotFound);

        var ext = Path.GetExtension(request.OriginalFileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => ".jpg",
            ".png" => ".png",
            ".webp" => ".webp",
            ".gif" => ".gif",
            _ => ".jpg"
        };
        var blobName = $"products/{request.ProductId.Value}/{Guid.NewGuid()}{ext}";

        await blobService.UploadAsync(blobName, request.ImageStream, request.ContentType, ct);

        product.SetImageBlob(blobName);
        await context.SaveChangesAsync(ct);

        await searchIndex.SyncAsync(product.Id.Value, ct);

        return Result<string>.Success(blobName);
    }
}
