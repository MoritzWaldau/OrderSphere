using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSphere.Catalog.Api.Models.Admin;
using OrderSphere.Catalog.Domain.Errors;
using OrderSphere.Catalog.Infrastructure.Persistence;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Catalog.Api.Features.Products.Admin;

public sealed class GetProductByIdAdminQueryHandler(CatalogDbContext context)
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
