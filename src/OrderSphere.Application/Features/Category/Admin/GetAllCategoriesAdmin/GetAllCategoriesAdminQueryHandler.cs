using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models.Admin;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Category.Admin.GetAllCategoriesAdmin;

public sealed class GetAllCategoriesAdminQueryHandler(
    IDbContext context,
    ILogger<GetAllCategoriesAdminQueryHandler> logger
) : IQueryHandler<GetAllCategoriesAdminQuery, Result<IReadOnlyList<AdminCategoryDto>>>
{
    public async Task<Result<IReadOnlyList<AdminCategoryDto>>> Handle(GetAllCategoriesAdminQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var categories = await context.Categories
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Name)
                .Select(c => new AdminCategoryDto(
                    c.Id,
                    c.Name,
                    c.Description,
                    c.IsActive,
                    context.Products.Count(p => p.CategoryId == c.Id && !p.IsDeleted),
                    c.CreatedAt,
                    c.UpdatedAt))
                .ToListAsync(cancellationToken);

            return Result<IReadOnlyList<AdminCategoryDto>>.Success(categories);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving categories for admin");
            return Result<IReadOnlyList<AdminCategoryDto>>.Failure(CategoryErrors.UnknownError);
        }
    }
}
