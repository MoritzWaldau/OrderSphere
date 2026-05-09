using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Product.Admin.CreateProduct;

public sealed class CreateProductCommandHandler(
    IDbContext context,
    ILogger<CreateProductCommandHandler> logger
) : ICommandHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var input = request.Input;

        if (input.Price <= 0)
            return Result<Guid>.Failure(ProductErrors.InvalidPrice);

        try
        {
            var product = new Domain.Entities.Product(
                input.Name,
                input.Description,
                input.Price,
                input.Stock,
                input.CategoryId,
                input.SKU);

            await context.BeginTransactionAsync(cancellationToken);
            await context.Products.AddAsync(product, cancellationToken);
            await context.CommitAsync(cancellationToken);

            logger.LogInformation("Product {ProductId} '{Name}' created", product.Id, product.Name);
            return Result<Guid>.Success(product.Id);
        }
        catch (Exception ex)
        {
            await context.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Failed to create product '{Name}'", input.Name);
            return Result<Guid>.Failure(ProductErrors.UnknownError);
        }
    }
}
