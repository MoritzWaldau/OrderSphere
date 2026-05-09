using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Category.GetCategories;

public sealed class GetCategoriesQueryHandler(
    IDbContext context,
    ILogger<GetCategoriesQueryHandler> logger
) : IQueryHandler<GetCategoriesQuery, Result<IReadOnlyList<CategoryDto>>>
{
    public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var categories = await context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive && !c.IsDeleted)
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto(
                    c.Id,
                    c.Name,
                    c.Description,
                    context.Products.Count(p => p.CategoryId == c.Id && p.IsActive && !p.IsDeleted)))
                .ToListAsync(cancellationToken);

            return Result<IReadOnlyList<CategoryDto>>.Success(categories);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving categories.");
            return Result<IReadOnlyList<CategoryDto>>.Failure(CategoryErrors.UnknownError);
        }
    }
}
