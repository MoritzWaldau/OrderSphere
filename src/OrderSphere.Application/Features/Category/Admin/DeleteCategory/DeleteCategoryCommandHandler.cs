using OrderSphere.Application.Abstraction;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Category.Admin.DeleteCategory;

public sealed class DeleteCategoryCommandHandler(ICatalogClient catalogClient)
    : ICommandHandler<DeleteCategoryCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(DeleteCategoryCommand request, CancellationToken ct)
        => catalogClient.DeleteCategoryAsync(request.CategoryId, ct);
}
