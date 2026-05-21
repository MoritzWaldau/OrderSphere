using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models.Admin;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Product.Admin.CreateProduct;

public sealed class CreateProductCommandHandler(ICatalogClient catalogClient)
    : ICommandHandler<CreateProductCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken ct)
        => catalogClient.CreateProductAsync(request.Input, ct);
}
