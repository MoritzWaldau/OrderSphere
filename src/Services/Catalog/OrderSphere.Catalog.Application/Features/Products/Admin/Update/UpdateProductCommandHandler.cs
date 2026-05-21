namespace OrderSphere.Catalog.Application.Features.Products.Admin.Update;

public sealed class UpdateProductCommandHandler(ICatalogDbContext context)
    : IRequestHandler<UpdateProductCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        throw new Exception();
    }
}
