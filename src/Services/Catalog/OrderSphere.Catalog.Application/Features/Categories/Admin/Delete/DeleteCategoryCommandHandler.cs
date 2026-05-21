namespace OrderSphere.Catalog.Application.Features.Categories.Admin.Delete;

public sealed class DeleteCategoryCommandHandler(ICatalogDbContext context)
    : IRequestHandler<DeleteCategoryCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        throw new Exception();
    }
}
