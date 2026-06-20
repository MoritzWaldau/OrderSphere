using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Catalog.Application.Features.Products.Admin.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    int Stock,
    CategoryId CategoryId,
    string SKU,
    string? ImageUrl,
    BrandId? BrandId = null) : ICommand<Result<Guid>>;
