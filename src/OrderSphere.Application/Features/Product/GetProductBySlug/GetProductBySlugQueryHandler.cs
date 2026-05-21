using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Product.GetProductBySlug;

public sealed class GetProductBySlugQueryHandler(ICatalogClient catalogClient)
    : IQueryHandler<GetProductBySlugQuery, Result<ProductDto>>
{
    public Task<Result<ProductDto>> Handle(GetProductBySlugQuery request, CancellationToken ct)
        => catalogClient.GetProductBySlugAsync(request.Slug, ct);
}
