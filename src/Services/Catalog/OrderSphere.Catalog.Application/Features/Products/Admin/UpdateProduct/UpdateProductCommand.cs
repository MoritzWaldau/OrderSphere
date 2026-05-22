namespace OrderSphere.Catalog.Application.Features.Products.Admin.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid ProductId,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    Guid CategoryId,
    string SKU,
    bool IsActive,
    string? ImageUrl) : IRequest<Result<bool>>;
