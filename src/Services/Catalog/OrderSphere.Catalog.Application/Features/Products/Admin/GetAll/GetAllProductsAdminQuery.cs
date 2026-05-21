namespace OrderSphere.Catalog.Application.Features.Products.Admin.GetAll;

public sealed record GetAllProductsAdminQuery : IRequest<Result<IEnumerable<AdminProductDto>>>;
