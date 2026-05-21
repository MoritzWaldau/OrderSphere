using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models.Admin;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Product.Admin.GetProductByIdAdmin;

public sealed class GetProductByIdAdminQueryHandler(ICatalogClient catalogClient)
    : IQueryHandler<GetProductByIdAdminQuery, Result<AdminProductDto>>
{
    public Task<Result<AdminProductDto>> Handle(GetProductByIdAdminQuery request, CancellationToken ct)
        => catalogClient.GetProductByIdAdminAsync(request.ProductId, ct);
}
