namespace OrderSphere.Catalog.Application.Features.Products.Admin.GetAllProductsAdmin;

public sealed record GetAllProductsAdminQuery : IQuery<Result<IEnumerable<AdminProductDto>>>;
