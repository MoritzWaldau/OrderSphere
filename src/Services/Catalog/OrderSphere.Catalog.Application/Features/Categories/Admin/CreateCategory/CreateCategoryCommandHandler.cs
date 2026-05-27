using OrderSphere.Catalog.Domain.Entities;

namespace OrderSphere.Catalog.Application.Features.Categories.Admin.CreateCategory;

public sealed class CreateCategoryCommandHandler(ICatalogDbContext context)
    : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var category = new Category(request.Name, request.Description);

        context.Categories.Add(category);
        await context.SaveChangesAsync(ct);

        return Result<Guid>.Success(category.Id.Value);
    }
}
