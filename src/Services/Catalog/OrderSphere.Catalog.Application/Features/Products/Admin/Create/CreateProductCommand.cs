namespace OrderSphere.Catalog.Application.Features.Products.Admin.Create;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    int Stock,
    Guid CategoryId,
    string SKU) : IRequest<Result<Guid>>;
