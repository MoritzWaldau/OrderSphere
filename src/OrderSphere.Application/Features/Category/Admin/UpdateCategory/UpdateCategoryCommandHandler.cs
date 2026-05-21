using OrderSphere.Application.Abstraction;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Category.Admin.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(ICatalogClient catalogClient)
    : ICommandHandler<UpdateCategoryCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(UpdateCategoryCommand request, CancellationToken ct)
        => catalogClient.UpdateCategoryAsync(request.CategoryId, request.Name, request.Description, request.IsActive, ct);
}
