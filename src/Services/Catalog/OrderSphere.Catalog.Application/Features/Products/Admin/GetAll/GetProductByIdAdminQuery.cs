namespace OrderSphere.Catalog.Application.Features.Products.Admin.GetAll;

public sealed record GetProductByIdAdminQuery(Guid ProductId) : IRequest<Result<AdminProductDto>>;
