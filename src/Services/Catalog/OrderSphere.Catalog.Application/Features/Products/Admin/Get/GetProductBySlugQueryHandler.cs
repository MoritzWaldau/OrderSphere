namespace OrderSphere.Catalog.Application.Features.Products.Admin.Get;

public sealed class GetProductBySlugQueryHandler(
    ICatalogDbContext context) : IRequestHandler<GetProductBySlugQuery, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(GetProductBySlugQuery request, CancellationToken ct)
    {
        throw new Exception();
    }
}
