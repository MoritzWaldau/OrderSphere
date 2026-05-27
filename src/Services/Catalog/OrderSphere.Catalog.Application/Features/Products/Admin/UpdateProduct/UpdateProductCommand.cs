using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Catalog.Application.Features.Products.Admin.UpdateProduct;

public sealed record UpdateProductCommand(
    ProductId ProductId,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    CategoryId CategoryId,
    string SKU,
    bool IsActive,
    string? ImageUrl) : IRequest<Result<bool>>;
