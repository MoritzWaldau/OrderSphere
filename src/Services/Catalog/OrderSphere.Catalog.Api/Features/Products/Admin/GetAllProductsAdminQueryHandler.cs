using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSphere.Catalog.Api.Models.Admin;
using OrderSphere.Catalog.Domain.Errors;
using OrderSphere.Catalog.Infrastructure.Persistence;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Catalog.Api.Features.Products.Admin;

public sealed class GetAllProductsAdminQueryHandler(CatalogDbContext context)
    : IRequestHandler<GetAllProductsAdminQuery, Result<IEnumerable<AdminProductDto>>>
{
    public async Task<Result<IEnumerable<AdminProductDto>>> Handle(GetAllProductsAdminQuery request, CancellationToken ct)
    {
        try
        {
            var products = await context.Products
                .Include(p => p.Category)
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.Name)
                .Select(p => new AdminProductDto(
                    p.Id, p.Name, p.Slug, p.Description, p.Price, p.Stock,
                    p.CategoryId, p.Category!.Name, p.SKU, p.IsActive, p.CreatedAt, p.UpdatedAt))
                .ToListAsync(ct);

            return Result<IEnumerable<AdminProductDto>>.Success(products);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<IEnumerable<AdminProductDto>>.Failure(ProductErrors.UnknownError);
        }
    }
}
