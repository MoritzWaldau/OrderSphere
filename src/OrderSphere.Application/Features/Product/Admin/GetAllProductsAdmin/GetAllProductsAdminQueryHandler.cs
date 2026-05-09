using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models.Admin;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Product.Admin.GetAllProductsAdmin;

public sealed class GetAllProductsAdminQueryHandler(
    IDbContext context,
    ILogger<GetAllProductsAdminQueryHandler> logger
) : IQueryHandler<GetAllProductsAdminQuery, Result<IReadOnlyList<AdminProductDto>>>
{
    public async Task<Result<IReadOnlyList<AdminProductDto>>> Handle(GetAllProductsAdminQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var products = await context.Products
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);

            var categoryIds = products.Select(p => p.CategoryId).Distinct().ToList();
            var categoryNames = await context.Categories
                .Where(c => categoryIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Name })
                .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

            var dtos = products.Select(p => new AdminProductDto(
                p.Id,
                p.Name,
                p.Slug,
                p.Description,
                p.Price,
                p.Stock,
                p.CategoryId,
                categoryNames.TryGetValue(p.CategoryId, out var name) ? name : "—",
                p.SKU,
                p.IsActive,
                p.CreatedAt,
                p.UpdatedAt)).ToList();

            return Result<IReadOnlyList<AdminProductDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving products for admin");
            return Result<IReadOnlyList<AdminProductDto>>.Failure(ProductErrors.UnknownError);
        }
    }
}
