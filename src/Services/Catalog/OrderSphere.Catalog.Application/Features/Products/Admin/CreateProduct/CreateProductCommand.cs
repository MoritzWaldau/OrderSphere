namespace OrderSphere.Catalog.Application.Features.Products.Admin.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    int Stock,
    Guid CategoryId,
    string SKU,
    string? ImageUrl) : IRequest<Result<Guid>>;
