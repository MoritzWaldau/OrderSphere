using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Product.GetProduct;

public sealed class GetProductQueryHandler(IDbContext context, ILogger<GetProductQueryHandler> logger) : IQueryHandler<GetProductQuery, Result<IEnumerable<ProductDto>>>
{
    public async Task<Result<IEnumerable<ProductDto>>> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var products = await context.Products.Where(p => p.Stock > 0)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Stock = p.Stock,
                })
                .ToListAsync(cancellationToken);

            return Result<IEnumerable<ProductDto>>.Success(products);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving products.");
            return Result<IEnumerable<ProductDto>>.Failure(ProductErrors.UnknownError);
        }
    }
}
