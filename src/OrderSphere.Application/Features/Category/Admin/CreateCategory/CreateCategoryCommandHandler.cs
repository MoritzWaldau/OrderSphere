using OrderSphere.Application.Abstraction;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Category.Admin.CreateCategory;

public sealed class CreateCategoryCommandHandler(ICatalogClient catalogClient)
    : ICommandHandler<CreateCategoryCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken ct)
        => catalogClient.CreateCategoryAsync(request.Name, request.Description, ct);
}
