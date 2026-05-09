using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Product.Admin.UpdateProduct;

public sealed class UpdateProductCommandHandler(
    IDbContext context,
    ILogger<UpdateProductCommandHandler> logger
) : ICommandHandler<UpdateProductCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var input = request.Input;

        if (input.Price <= 0)
            return Result<bool>.Failure(ProductErrors.InvalidPrice);

        try
        {
            var product = await context.Products
                .FirstOrDefaultAsync(p => p.Id == request.ProductId && !p.IsDeleted, cancellationToken);

            if (product is null)
                return Result<bool>.Failure(ProductErrors.ProductNotFoundError);

            product.UpdateDetails(input.Name, input.Description, input.Price, input.Stock, input.CategoryId, input.SKU);

            if (request.IsActive && !product.IsActive)
                product.Activate();
            else if (!request.IsActive && product.IsActive)
                product.Deactivate();

            context.Products.Update(product);
            await context.BeginTransactionAsync(cancellationToken);
            await context.CommitAsync(cancellationToken);

            logger.LogInformation("Product {ProductId} updated", product.Id);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await context.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Failed to update product {ProductId}", request.ProductId);
            return Result<bool>.Failure(ProductErrors.UnknownError);
        }
    }
}
