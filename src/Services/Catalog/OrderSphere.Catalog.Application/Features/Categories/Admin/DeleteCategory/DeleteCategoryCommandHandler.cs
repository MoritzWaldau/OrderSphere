using Microsoft.EntityFrameworkCore;
using OrderSphere.Catalog.Domain.Errors;

namespace OrderSphere.Catalog.Application.Features.Categories.Admin.DeleteCategory;

public sealed class DeleteCategoryCommandHandler(ICatalogDbContext context)
    : ICommandHandler<DeleteCategoryCommand, Result>
{
    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var category = await context.Categories
            .AsTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && !c.IsDeleted, ct);

        if (category is null)
            return Result.Failure(CategoryErrors.NotFound);

        var hasProducts = await context.Products
            .AsNoTracking()
            .AnyAsync(p => p.CategoryId == request.CategoryId && !p.IsDeleted, ct);

        if (hasProducts)
            return Result.Failure(CategoryErrors.HasProducts);

        category.Delete();

        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
