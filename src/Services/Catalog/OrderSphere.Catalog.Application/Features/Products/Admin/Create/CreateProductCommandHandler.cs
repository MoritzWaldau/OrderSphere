namespace OrderSphere.Catalog.Application.Features.Products.Admin.Create;

public sealed class CreateProductCommandHandler(ICatalogDbContext context)
    : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        throw new Exception();
    }
}
