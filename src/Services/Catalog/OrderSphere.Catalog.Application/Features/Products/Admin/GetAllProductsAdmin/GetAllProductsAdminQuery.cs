namespace OrderSphere.Catalog.Application.Features.Products.Admin.GetAllProductsAdmin;

public sealed record GetAllProductsAdminQuery : IRequest<Result<IEnumerable<AdminProductDto>>>;
