using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models.Admin;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Product.Admin.GetProductByIdAdmin;

public sealed class GetProductByIdAdminQueryHandler(ICatalogClient catalogClient)
    : IQueryHandler<GetProductByIdAdminQuery, Result<AdminProductDto>>
{
    public Task<Result<AdminProductDto>> Handle(GetProductByIdAdminQuery request, CancellationToken ct)
        => catalogClient.GetProductByIdAdminAsync(request.ProductId, ct);
}
