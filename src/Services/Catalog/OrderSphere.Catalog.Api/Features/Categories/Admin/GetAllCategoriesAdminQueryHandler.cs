using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSphere.Catalog.Api.Models.Admin;
using OrderSphere.Catalog.Domain.Errors;
using OrderSphere.Catalog.Infrastructure.Persistence;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Catalog.Api.Features.Categories.Admin;

public sealed class GetAllCategoriesAdminQueryHandler(CatalogDbContext context)
    : IRequestHandler<GetAllCategoriesAdminQuery, Result<IEnumerable<AdminCategoryDto>>>
{
    public async Task<Result<IEnumerable<AdminCategoryDto>>> Handle(GetAllCategoriesAdminQuery request, CancellationToken ct)
    {
        try
        {
            var categories = await context.Categories
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Name)
                .Select(c => new AdminCategoryDto(
                    c.Id, c.Name, c.Description, c.IsActive,
                    context.Products.Count(p => p.CategoryId == c.Id && !p.IsDeleted),
                    c.CreatedAt, c.UpdatedAt))
                .ToListAsync(ct);

            return Result<IEnumerable<AdminCategoryDto>>.Success(categories);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<IEnumerable<AdminCategoryDto>>.Failure(CategoryErrors.UnknownError);
        }
    }
}
