namespace OrderSphere.Catalog.Application.Features.Products.Admin.Delete;

public sealed class DeleteProductCommandHandler(ICatalogDbContext context)
    : IRequestHandler<DeleteProductCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        throw new Exception();
    }
}
