using Microsoft.EntityFrameworkCore;
using OrderSphere.Catalog.Domain.Errors;

namespace OrderSphere.Catalog.Application.Features.Products.Admin.DeleteProduct;

public sealed class DeleteProductCommandHandler(ICatalogDbContext context)
    : IRequestHandler<DeleteProductCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        var product = await context.Products
            .AsTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && !p.IsDeleted, ct);

        if (product is null)
            return Result<bool>.Failure(ProductErrors.NotFound);

        product.IsDeleted = true;
        product.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }
}
