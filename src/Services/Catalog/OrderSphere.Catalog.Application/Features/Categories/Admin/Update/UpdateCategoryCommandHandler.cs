namespace OrderSphere.Catalog.Application.Features.Categories.Admin.Update;

public sealed class UpdateCategoryCommandHandler(ICatalogDbContext context)
    : IRequestHandler<UpdateCategoryCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        throw new Exception();
    }
}
