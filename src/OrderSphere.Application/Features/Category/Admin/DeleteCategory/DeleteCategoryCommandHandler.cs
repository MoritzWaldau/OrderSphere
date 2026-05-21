using OrderSphere.Application.Abstraction;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Category.Admin.DeleteCategory;

public sealed class DeleteCategoryCommandHandler(ICatalogClient catalogClient)
    : ICommandHandler<DeleteCategoryCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(DeleteCategoryCommand request, CancellationToken ct)
        => catalogClient.DeleteCategoryAsync(request.CategoryId, ct);
}
