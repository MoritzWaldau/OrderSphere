namespace OrderSphere.Catalog.Application.Features.Products.Admin.Delete;

public sealed record DeleteProductCommand(Guid ProductId) : IRequest<Result<bool>>;
