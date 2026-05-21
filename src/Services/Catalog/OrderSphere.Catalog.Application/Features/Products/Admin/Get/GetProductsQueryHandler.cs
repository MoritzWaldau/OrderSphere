namespace OrderSphere.Catalog.Application.Features.Products.Admin.Get;

public sealed class GetProductsQueryHandler(ICatalogDbContext context)
    : IRequestHandler<GetProductsQuery, Result<PagedResult<ProductDto>>>
{
    public async Task<Result<PagedResult<ProductDto>>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        throw new Exception();
    }
}
