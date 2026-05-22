namespace OrderSphere.Catalog.Application.Features.Products.Admin.GetProductByIdAdmin;

public sealed record GetProductByIdAdminQuery(Guid ProductId) : IRequest<Result<AdminProductDto>>;
