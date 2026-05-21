using MediatR;
using OrderSphere.Catalog.Application.Abstractions;
using OrderSphere.Catalog.Application.Caching;
using OrderSphere.Catalog.Domain.Entities;


namespace OrderSphere.Catalog.Application.Features.Categories.Admin.Create;

public sealed class CreateCategoryCommandHandler(ICatalogDbContext context)
    : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        return (Result<Guid>)await Task.FromResult(Result<Guid>.Success());
    }
}
