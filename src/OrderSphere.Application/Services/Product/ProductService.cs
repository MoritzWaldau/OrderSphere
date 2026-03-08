using OrderSphere.Application.Repositories;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Outbox;
using OrderSphere.Domain.Primitives;
using System.Text.Json;

namespace OrderSphere.Application.Services.Product;

public sealed class ProductService(IUnitOfWork UnitOfWork) : IProductService
{
    public async Task<Result<Domain.Entities.Product>> CreateProductAsync(string name, string description, decimal price, int stock)
    {
        if (price <= 0)
            return Result<Domain.Entities.Product>.Failure(ProductErrors.InvalidPrice);

        var existing = await UnitOfWork.Products.GetByNameAsync(name);
        if (existing != null)
            return Result<Domain.Entities.Product>.Failure(ProductErrors.NameAlreadyExists);

        var product = new Domain.Entities.Product(name, description, price, stock);
        await UnitOfWork.Products.AddAsync(product);

        var @event = new { product.Id, product.Name, product.Price, product.Stock, product.CreatedAt };
        var outbox = new OutboxEvent
        {
            Type = "ProductCreatedEvent",
            Payload = JsonSerializer.Serialize(@event)
        };

        await UnitOfWork.Outbox.AddAsync(outbox);

        await UnitOfWork.CommitAsync();
        return Result<Domain.Entities.Product>.Success(product);
    }
}
