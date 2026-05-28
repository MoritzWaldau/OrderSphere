using Microsoft.EntityFrameworkCore;
using OrderSphere.Catalog.Domain.Errors;

namespace OrderSphere.Catalog.Application.Features.Categories.Admin.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(ICatalogDbContext context)
    : ICommandHandler<UpdateCategoryCommand, Result>
{
    public async Task<Result> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var category = await context.Categories
            .AsTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && !c.IsDeleted, ct);

        if (category is null)
            return Result.Failure(CategoryErrors.NotFound);

        category.UpdateDetails(request.Name, request.Description);

        if (request.IsActive)
            category.Activate();
        else
            category.Deactivate();

        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
