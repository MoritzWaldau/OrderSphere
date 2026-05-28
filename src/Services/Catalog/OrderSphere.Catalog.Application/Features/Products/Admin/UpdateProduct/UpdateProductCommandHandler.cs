using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.ValueObjects;
using OrderSphere.Catalog.Domain.Errors;

namespace OrderSphere.Catalog.Application.Features.Products.Admin.UpdateProduct;

public sealed class UpdateProductCommandHandler(ICatalogDbContext context)
    : ICommandHandler<UpdateProductCommand, Result>
{
    public async Task<Result> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await context.Products
            .AsTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && !p.IsDeleted, ct);

        if (product is null)
            return Result.Failure(ProductErrors.NotFound);

        product.UpdateDetails(
            request.Name,
            request.Description,
            Money.Of(request.Price),
            request.Stock,
            request.CategoryId,
            request.SKU,
            request.ImageUrl);

        if (request.IsActive)
            product.Activate();
        else
            product.Deactivate();

        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
