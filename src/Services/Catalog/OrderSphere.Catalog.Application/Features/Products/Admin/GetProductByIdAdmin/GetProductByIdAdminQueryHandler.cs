namespace OrderSphere.Catalog.Application.Features.Products.Admin.GetProductByIdAdmin;

public sealed class GetProductByIdAdminQueryHandler(ICatalogDbContext context)
    : IRequestHandler<GetProductByIdAdminQuery, Result<AdminProductDto>>
{
    public async Task<Result<AdminProductDto>> Handle(GetProductByIdAdminQuery request, CancellationToken ct)
    {
        var product = await context.Products
            .Include(p => p.Category)
            .Where(p => p.Id == request.ProductId && !p.IsDeleted)
            .Select(p => new AdminProductDto(
                p.Id, p.Name, p.Slug, p.Description, p.Price, p.Stock,
                p.CategoryId, p.Category!.Name, p.SKU, p.IsActive, p.CreatedAt, p.UpdatedAt))
            .FirstOrDefaultAsync(ct);

        return product is null
            ? Result<AdminProductDto>.Failure(ProductErrors.NotFound)
            : Result<AdminProductDto>.Success(product);
    }
}
