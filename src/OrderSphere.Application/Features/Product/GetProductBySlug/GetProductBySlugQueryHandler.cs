using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Features.Product.GetProduct;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderSphere.Application.Features.Product.GetProductBySlug;

public sealed class GetProductBySlugQueryHandler(
    IDbContext context, 
    ILogger<GetProductBySlugQueryHandler> logger
    ) : IQueryHandler<GetProductBySlugQuery, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(GetProductBySlugQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var product = await context.Products
                .Include(p => p.Category)
                .Where(p => p.Stock > 0 && p.IsActive)
                .FirstOrDefaultAsync(p => p.Slug == request.Slug, cancellationToken);

            if (product == null)
            {
                return Result<ProductDto>.Failure(ProductErrors.ProductNotFoundError);
            }
          
            var productDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                CategoryId = product.CategoryId,
                CategoryName = product.Category!.Name,
                SKU = product.SKU,
                IsActive = product.IsActive,
            };

            return Result<ProductDto>.Success(productDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving products.");
            return Result<ProductDto>.Failure(ProductErrors.UnknownError);
        }
    }
}
