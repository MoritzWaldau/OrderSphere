using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using OrderSphere.Catalog.Api.Caching;
using OrderSphere.Catalog.Domain.Entities;
using OrderSphere.Catalog.Domain.Errors;
using OrderSphere.Catalog.Infrastructure.Persistence;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Catalog.Api.Features.Categories.Admin;

public sealed class CreateCategoryCommandHandler(CatalogDbContext context, HybridCache cache)
    : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        try
        {
            var category = new Category(request.Name, request.Description);
            context.Categories.Add(category);
            await context.SaveChangesAsync(ct);
            await cache.RemoveByTagAsync(CatalogCache.Tag, ct);
            return Result<Guid>.Success(category.Id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<Guid>.Failure(CategoryErrors.UnknownError);
        }
    }
}
