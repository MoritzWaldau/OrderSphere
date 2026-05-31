using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Catalog.Application.Features.Products.Admin.GetProductByIdAdmin;

public sealed record GetProductByIdAdminQuery(ProductId ProductId) : IQuery<Result<AdminProductDto>>;
