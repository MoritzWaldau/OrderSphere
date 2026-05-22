namespace OrderSphere.Catalog.Application.Features.Products.Admin.DeleteProduct;

public sealed record DeleteProductCommand(Guid ProductId) : IRequest<Result<bool>>;
