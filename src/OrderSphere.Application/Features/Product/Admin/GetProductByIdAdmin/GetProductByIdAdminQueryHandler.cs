using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models.Admin;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Product.Admin.GetProductByIdAdmin;

public sealed class GetProductByIdAdminQueryHandler(
    IDbContext context,
    ILogger<GetProductByIdAdminQueryHandler> logger
) : IQueryHandler<GetProductByIdAdminQuery, Result<AdminProductDto>>
{
    public async Task<Result<AdminProductDto>> Handle(GetProductByIdAdminQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var product = await context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.ProductId && !p.IsDeleted, cancellationToken);

            if (product is null)
                return Result<AdminProductDto>.Failure(ProductErrors.ProductNotFoundError);

            var categoryName = await context.Categories
                .Where(c => c.Id == product.CategoryId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? "—";

            var dto = new AdminProductDto(
                product.Id,
                product.Name,
                product.Slug,
                product.Description,
                product.Price,
                product.Stock,
                product.CategoryId,
                categoryName,
                product.SKU,
                product.IsActive,
                product.CreatedAt,
                product.UpdatedAt);

            return Result<AdminProductDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving product {ProductId} for admin", request.ProductId);
            return Result<AdminProductDto>.Failure(ProductErrors.UnknownError);
        }
    }
}
