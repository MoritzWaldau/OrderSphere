using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Catalog.Application.Features.Products.Admin.UploadProductImage;

public sealed record UploadProductImageCommand(
    ProductId ProductId,
    Stream ImageStream,
    string ContentType,
    string OriginalFileName) : ICommand<Result<string>>;
